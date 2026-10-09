using System.Diagnostics;
using System.Text;
using System.Text.Json;
using FleetVerification;

// Requires the .NET 10 SDK and Docker Compose. No shell-specific commands.
if (args is ["--validate-results", var archivedResults])
{
    try
    {
        TestResults.Verify(Path.GetFullPath(archivedResults));
        return 0;
    }
    catch (Exception error)
    {
        Console.Error.WriteLine("FAIL: " + error.Message);
        return 1;
    }
}

var coldBuild = args is ["--cold-build"];
if (args.Length != 0 && !coldBuild)
{
    Console.Error.WriteLine("Usage: dotnet run --project tools/VerifyTests/VerifyTests.csproj --configuration Release -- [--cold-build]");
    return 1;
}

var root = FindRoot();
var runId = Guid.NewGuid().ToString("N");
var project = "fleet-tests-" + runId;
var runDirectory = Path.Combine(root, ".scratch", "full-suite", runId);
Directory.CreateDirectory(runDirectory);
var resultsDirectory = Path.Combine(runDirectory, "results");
var overrideFile = Path.Combine(runDirectory, "compose.override.yml");
await File.WriteAllTextAsync(overrideFile, """
services:
  postgres:
    ports: !override ["127.0.0.1::5432"]
  redis:
    ports: !override ["127.0.0.1::6379"]
  identity:
    ports: !override ["127.0.0.1::8080"]
  application:
    ports: !override ["127.0.0.1::8080", "127.0.0.1::8081"]
""");
var compose = new[]
{
    "compose",
    "-p",
    project,
    "-f",
    Path.Combine(root, "docker-compose.yml"),
    "-f",
    overrideFile
};
// Existing installation import values must not enter the fresh test environment.
var dockerEnvironment = new Dictionary<string, string?>
{
    ["FLEET_POSTGRES_USER"] = null,
    ["FLEET_POSTGRES_PASSWORD"] = null,
    ["FLEET_REDIS_PASSWORD"] = null,
    ["FLEET_RUNTIME_POSTGRES_PASSWORD"] = null
};
using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancellation.Cancel();
};
var started = false;
var builderStarted = false;
var exitCode = 0;
try
{
    Console.WriteLine($"Isolated verification project: {project}; fresh volumes and automatic host ports.");
    var versionText = (await Run("docker", ["compose", "version", "--short"], capture: true)).Trim().TrimStart('v');
    var numericVersion = versionText.Split('-')[0];
    if (!Version.TryParse(numericVersion, out var composeVersion) || composeVersion < new Version(2, 24, 4))
        throw new InvalidOperationException("Docker Compose 2.24.4 or later is required for port overrides.");
    if ((await Run("docker", ["info", "--format", "{{.OSType}}"], capture: true)).Trim() != "linux")
        throw new InvalidOperationException("Switch Docker to Linux containers before running verification.");
    var existingContainers = await Run("docker", ["ps", "-aq", "--filter", "label=com.docker.compose.project=" + project], capture: true);
    var existingVolumes = await Run("docker", ["volume", "ls", "-q", "--filter", "label=com.docker.compose.project=" + project], capture: true);
    if (!string.IsNullOrWhiteSpace(existingContainers) || !string.IsNullOrWhiteSpace(existingVolumes))
        throw new InvalidOperationException("Refusing to reuse an existing verification project.");
    await Compose(["config", "--quiet"]);
    if (coldBuild)
    {
        var buildxText = await Run("docker", ["buildx", "version"], capture: true);
        var buildxMatch = System.Text.RegularExpressions.Regex.Match(buildxText, @"\bv(\d+\.\d+\.\d+)");
        if (!buildxMatch.Success || !Version.TryParse(buildxMatch.Groups[1].Value, out var buildxVersion) || buildxVersion < new Version(0, 14, 0))
            throw new InvalidOperationException("Cold verification requires Docker Buildx 0.14 or later.");
        var existingBuilders = await Run("docker", ["buildx", "ls", "--format", "{{.Name}}"], capture: true);
        if (existingBuilders.Split('\n').Any(name => name.Trim().TrimEnd('*') == project))
            throw new InvalidOperationException("Refusing to reuse an existing verification builder.");
        builderStarted = true; // Remove even partially-created builder state on failure.
        await Run("docker", ["buildx", "create", "--name", project, "--driver", "docker-container", "--driver-opt", "default-load=true"]);
    }
    started = true; // Clean up generated images even when build or up only partially succeeds.
    // Build current source into unique project images. Cached layers do not reuse test data/results.
    await Compose(coldBuild ? ["build", "--builder", project] : ["build"], timeoutMinutes: 30);
    await Compose(["up", "--no-build", "-d"]);
    var restPort = await PublishedPort("application", "8080");
    var postgresPort = await PublishedPort("postgres", "5432");
    var redisPort = await PublishedPort("redis", "6379");
    using var http = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(3)
    };
    var readyUntil = DateTime.UtcNow.AddMinutes(3);
    var ready = false;
    while (DateTime.UtcNow < readyUntil)
    {
        cancellation.Token.ThrowIfCancellationRequested();
        try
        {
            ready = await http.GetStringAsync($"http://127.0.0.1:{restPort}/health/ready", cancellation.Token) == "Healthy";
        }
        catch (HttpRequestException)
        {
        }
        catch (OperationCanceledException)when (!cancellation.IsCancellationRequested)
        {
        }

        if (ready)
            break;
        await Task.Delay(TimeSpan.FromSeconds(2), cancellation.Token);
    }

    if (!ready)
        throw new InvalidOperationException("Fresh application did not become ready after bootstrap and migrations.");
    Console.WriteLine("PASS: Fresh database, bootstrap, migrations and readiness.");
    // Capture the private configuration in memory; never echo it or write it on the host.
    var configurationText = await Compose(["exec", "-T", "application", "cat", "/run/fleet/appsettings.local.json"], capture: true);
    using var configuration = JsonDocument.Parse(configurationText);
    var connections = configuration.RootElement.GetProperty("ConnectionStrings");
    var postgres = connections.GetProperty("MigrationPostgreSql").GetString()!.Replace("Host=postgres;", $"Host=127.0.0.1;Port={postgresPort};", StringComparison.Ordinal);
    var redis = connections.GetProperty("Redis").GetString()!.Replace("redis:6379,", $"127.0.0.1:{redisPort},", StringComparison.Ordinal);
    var testEnvironment = new Dictionary<string, string?>
    {
        ["FLEET_TEST_POSTGRES_CONNECTION"] = postgres,
        ["FLEET_TEST_REDIS_CONNECTION"] = redis,
        ["ConnectionStrings__PostgreSql"] = postgres,
        ["ConnectionStrings__Redis"] = redis,
        ["LocalEnvironment__ConfigurationFile"] = null
    };
    await Run("dotnet", ["test", "FleetCompany.FleetManagement.Backend.sln", "--configuration", "Release", "--verbosity", "minimal", "--logger", "trx", "--results-directory", resultsDirectory], environment: testEnvironment);
    TestResults.Verify(resultsDirectory);
    Console.WriteLine("Results retained at: " + Path.GetRelativePath(root, resultsDirectory));
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("FAIL: Verification cancelled; cleaning up its isolated environment.");
    exitCode = 1;
}
catch (Exception error)
{
    Console.Error.WriteLine("FAIL: " + error.Message);
    exitCode = 1;
}
finally
{
    if (started)
    {
        try
        {
            // Only the fresh generated project is removed; never the user's development volumes.
            await Compose(["down", "--volumes", "--remove-orphans", "--rmi", "local"], cleanup: true);
            Console.WriteLine("PASS: Isolated test containers, volumes and generated images removed; development environment untouched.");
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("FAIL: Cleanup for " + project + ": " + error.Message);
            exitCode = 1;
        }
    }

    if (builderStarted)
    {
        try
        {
            // No --use, --keep-state or global prune: other builders and caches stay intact.
            await Run("docker", ["buildx", "rm", "--force", project], cleanup: true);
            Console.WriteLine("PASS: Dedicated test builder and its build cache removed.");
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("FAIL: Builder cleanup for " + project + ": " + error.Message);
            exitCode = 1;
        }
    }
}

return exitCode;
Task<string> Compose(string[] command, bool capture = false, bool cleanup = false, int timeoutMinutes = 10) => Run("docker", [..compose, ..command], capture, dockerEnvironment, cleanup, timeoutMinutes);
async Task<int> PublishedPort(string service, string containerPort)
{
    var binding = (await Compose(["port", service, containerPort], capture: true)).Trim();
    var port = int.Parse(binding[(binding.LastIndexOf(':') + 1)..], System.Globalization.CultureInfo.InvariantCulture);
    if (port is < 1 or > 65535)
        throw new InvalidOperationException("Invalid published test port.");
    return port;
}

async Task<string> Run(string executable, string[] arguments, bool capture = false, IReadOnlyDictionary<string, string?>? environment = null, bool cleanup = false, int timeoutMinutes = 10)
{
    var start = new ProcessStartInfo(executable)
    {
        WorkingDirectory = root,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    };
    foreach (var argument in arguments)
        start.ArgumentList.Add(argument);
    if (environment is not null)
        foreach (var(name, value)in environment)
            if (value is null)
                start.Environment.Remove(name);
            else
                start.Environment[name] = value;
    using var process = Process.Start(start) ?? throw new InvalidOperationException("Unable to start " + executable);
    var output = new StringBuilder();
    async Task Pump(StreamReader stream, bool stderr)
    {
        while (await stream.ReadLineAsync()is { } line)
        {
            if (capture)
            {
                if (!stderr)
                    output.AppendLine(line);
            }
            else if (stderr)
                Console.Error.WriteLine(line);
            else
                Console.WriteLine(line);
        }
    }

    var stdout = Pump(process.StandardOutput, false);
    var stderr = Pump(process.StandardError, true);
    var limitMinutes = cleanup ? 2 : timeoutMinutes;
    using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(limitMinutes));
    using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cleanup ? CancellationToken.None : cancellation.Token);
    try
    {
        await process.WaitForExitAsync(linked.Token);
    }
    catch (OperationCanceledException)
    {
        if (!process.HasExited)
            process.Kill(entireProcessTree: true);
        await process.WaitForExitAsync();
        await Task.WhenAll(stdout, stderr);
        if (timeout.IsCancellationRequested && !cancellation.IsCancellationRequested)
            throw new TimeoutException($"{executable} command exceeded {limitMinutes} minutes.");
        throw;
    }

    await Task.WhenAll(stdout, stderr);
    if (process.ExitCode != 0)
        throw new InvalidOperationException($"{executable} command exited with code {process.ExitCode}.");
    return output.ToString();
}

static string FindRoot()
{
    for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        if (File.Exists(Path.Combine(directory.FullName, ".mpcore", "template-manifest.json")))
            return directory.FullName;
    throw new InvalidOperationException("Run the verifier from a Fleet Management source checkout.");
}
