using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FleetVerification;

// Development-only. Private Docker volumes and child environment; no credential file or User Secrets writes.
using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancellation.Cancel();
};
try
{
    var mode = args.FirstOrDefault() ?? "run";
    if (mode is not ("run" or "verify" or "down"))
        throw new InvalidOperationException("Modes: run, verify, down; optional --project NAME, --no-build.");
    var project = "fleetmanagement-dependencies";
    var noBuild = false;
    for (var i = 1; i < args.Length; i++)
        if (args[i] == "--no-build")
            noBuild = true;
        else if (args[i] == "--project" && ++i < args.Length)
            project = args[i];
        else
            throw new InvalidOperationException("Unknown or incomplete option.");
    if (!Regex.IsMatch(project, @"^fleetmanagement-dependencies(?:-[a-z0-9]+)?$"))
        throw new InvalidOperationException("Project must be fleetmanagement-dependencies or that name with a lowercase suffix. Refusing to reuse the full Compose project.");
    var root = FindRoot();
    int Port(string key, int fallback)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (value is null)
            return fallback;
        if (!int.TryParse(value, out var port) || port is < 1 or > 65535)
            throw new InvalidOperationException("Invalid port: " + key);
        return port;
    }

    var pg = Port("FLEET_DEPENDENCIES_POSTGRES_PORT", 56432);
    var redis = Port("FLEET_DEPENDENCIES_REDIS_PORT", 57379);
    var identity = Port("FLEET_DEPENDENCIES_IDENTITY_PORT", 56480);
    var rest = Port("FLEET_LOCAL_REST_PORT", 8180);
    var grpc = Port("FLEET_LOCAL_GRPC_PORT", 8181);
    if (new[]
    {
        pg,
        redis,
        identity,
        rest,
        grpc
    }.Distinct().Count() != 5)
        throw new InvalidOperationException("Local ports must be distinct.");
    var dockerEnvironment = new Dictionary<string, string?>
    {
        ["FLEET_POSTGRES_USER"] = null,
        ["FLEET_POSTGRES_PASSWORD"] = null,
        ["FLEET_RUNTIME_POSTGRES_PASSWORD"] = null,
        ["FLEET_REDIS_PASSWORD"] = null,
        ["FLEET_DEPENDENCIES_POSTGRES_PORT"] = pg.ToString(),
        ["FLEET_DEPENDENCIES_REDIS_PORT"] = redis.ToString(),
        ["FLEET_DEPENDENCIES_IDENTITY_PORT"] = identity.ToString()
    };
    string[] compose = ["compose", "-p", project, "-f", Path.Combine(root, "docker-compose.dependencies.yml")];
    Task<string> Compose(string[] command, bool capture = false) => Execute("docker", [..compose, ..command], root, dockerEnvironment, cancellation.Token, capture);
    if (mode == "down")
    {
        await Compose(["down"]);
        return 0;
    } // Keep data, keys and config together.

    var versionText = (await Execute("docker", ["compose", "version", "--short"], root, dockerEnvironment, cancellation.Token, true)).Trim().TrimStart('v').Split('-')[0];
    if (!Version.TryParse(versionText, out var version) || version < new Version(2, 24, 4))
        throw new InvalidOperationException("Docker Compose 2.24.4 or later is required.");
    if ((await Execute("docker", ["info", "--format", "{{.OSType}}"], root, dockerEnvironment, cancellation.Token, true)).Trim() != "linux")
        throw new InvalidOperationException("Docker must use Linux containers.");
    await Compose(["config", "--quiet"]);
    await Compose(["up", noBuild ? "--no-build" : "--build", "-d", "--wait", "--wait-timeout", "180"]);
    var text = await Compose(["exec", "-T", "redis", "cat", "/run/fleet/appsettings.local.json"], true);
    using var configuration = JsonDocument.Parse(text);
    var connections = configuration.RootElement.GetProperty("ConnectionStrings");
    string Postgres(string key) => connections.GetProperty(key).GetString()!.Replace("Host=postgres;", $"Host=127.0.0.1;Port={pg};", StringComparison.Ordinal);
    var environment = new Dictionary<string, string?>
    {
        ["ASPNETCORE_ENVIRONMENT"] = "Development",
        ["DOTNET_ENVIRONMENT"] = "Development",
        ["LocalEnvironment__ConfigurationFile"] = null,
        ["ConnectionStrings__PostgreSql"] = Postgres("PostgreSql"),
        ["ConnectionStrings__MigrationPostgreSql"] = Postgres("MigrationPostgreSql"),
        ["ConnectionStrings__Redis"] = connections.GetProperty("Redis").GetString()!.Replace("redis:6379,", $"127.0.0.1:{redis},", StringComparison.Ordinal),
        ["Security__Authority"] = $"http://127.0.0.1:{identity}",
        ["Security__Audiences__0"] = "fleet-management",
        ["Security__RequireHttpsMetadata"] = "false",
        ["Database__ApplyMigrations"] = "true",
        ["Kestrel__Endpoints__Rest__Url"] = $"http://127.0.0.1:{rest}",
        ["Kestrel__Endpoints__Grpc__Url"] = $"http://127.0.0.1:{grpc}",
        ["Transport__RestPort"] = rest.ToString(),
        ["Transport__GrpcPort"] = grpc.ToString()
    };
    Console.WriteLine($"Dependencies: {project}; REST http://127.0.0.1:{rest}; gRPC http://127.0.0.1:{grpc}. No User Secrets modified.");
    // On Windows a running host locks its DLLs. Build every consumer before starting it.
    await Execute("dotnet", ["build", "FleetCompany.FleetManagement.Backend.sln", "--configuration", "Release", "--verbosity", "minimal"], root, environment, cancellation.Token);
    if (mode == "verify")
    {
        await Execute("dotnet", ["build", "tools/SmokeClient", "--configuration", "Release", "--verbosity", "minimal"], root, environment, cancellation.Token);
        environment["Logging__LogLevel__Default"] = "Warning";
    }

    using var hostCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token);
    var host = Execute("dotnet", ["run", "--project", "src/FleetCompany.FleetManagement.Api", "--configuration", "Release", "--no-build", "--no-launch-profile"], root, environment, hostCancellation.Token, unlimited: true);
    if (mode == "run")
    {
        await host;
        return 0;
    }

    try
    {
        using var http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(3)
        };
        var deadline = DateTime.UtcNow.AddMinutes(3);
        var ready = false;
        while (DateTime.UtcNow < deadline)
        {
            cancellation.Token.ThrowIfCancellationRequested();
            if (host.IsCompleted)
            {
                await host;
                throw new InvalidOperationException("Host stopped before readiness.");
            }

            try
            {
                ready = await http.GetStringAsync($"http://127.0.0.1:{rest}/health/ready", cancellation.Token) == "Healthy";
            }
            catch (HttpRequestException)
            {
            }
            catch (OperationCanceledException)when (!cancellation.IsCancellationRequested)
            {
            }

            if (ready)
                break;
            await Task.Delay(1000, cancellation.Token);
        }

        if (!ready)
            throw new InvalidOperationException("Local host did not become ready.");
        Console.WriteLine("PASS: Native host readiness and automatic migrations.");
        environment["FLEET_SMOKE_REST_URI"] = $"http://127.0.0.1:{rest}";
        environment["FLEET_SMOKE_GRPC_URI"] = $"http://127.0.0.1:{grpc}";
        environment["FLEET_SMOKE_IDENTITY_URI"] = environment["Security__Authority"];
        environment["FLEET_SMOKE_POSTGRES_PORT"] = pg.ToString();
        environment["FLEET_CONTAINER_POSTGRES_CONNECTION"] = connections.GetProperty("PostgreSql").GetString();
        await Execute("dotnet", ["run", "--project", "tools/SmokeClient", "--configuration", "Release", "--no-build"], root, environment, cancellation.Token);
        // TestServer uses Testing and HTTPS metadata with its own signed JWT fixture.
        // Never leak Development-only HTTP metadata or native listener options into it.
        var testEnvironment = new Dictionary<string, string?>
        {
            ["FLEET_TEST_POSTGRES_CONNECTION"] = environment["ConnectionStrings__MigrationPostgreSql"],
            ["FLEET_TEST_REDIS_CONNECTION"] = environment["ConnectionStrings__Redis"],
            ["ConnectionStrings__PostgreSql"] = environment["ConnectionStrings__MigrationPostgreSql"],
            ["ConnectionStrings__Redis"] = environment["ConnectionStrings__Redis"],
            ["LocalEnvironment__ConfigurationFile"] = null,
            ["ASPNETCORE_ENVIRONMENT"] = "Testing",
            ["DOTNET_ENVIRONMENT"] = "Testing",
            ["Security__RequireHttpsMetadata"] = "true",
            ["Database__ApplyMigrations"] = "false"
        };
        var results = Path.Combine(root, ".scratch", "local-development", Guid.NewGuid().ToString("N"), "results");
        await Execute("dotnet", ["test", "FleetCompany.FleetManagement.Backend.sln", "--configuration", "Release", "--no-build", "--verbosity", "minimal", "--logger", "trx", "--results-directory", results], root, testEnvironment, cancellation.Token);
        TestResults.Verify(results);
        Console.WriteLine("Results retained at " + results);
    }
    finally
    {
        hostCancellation.Cancel();
        try
        {
            await host;
        }
        catch (OperationCanceledException)
        {
        }
    }

    return 0;
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Host stopped; dependencies and volumes retained.");
    return 130;
}
catch (Exception error)
{
    Console.Error.WriteLine("FAIL: " + (error is InvalidOperationException ? error.Message : error.GetType().Name));
    return 1;
}

static async Task<string> Execute(string executable, string[] arguments, string root, IReadOnlyDictionary<string, string?> environment, CancellationToken cancellation, bool capture = false, bool unlimited = false)
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
    foreach (var(key, value)in environment)
        if (value is null)
            start.Environment.Remove(key);
        else
            start.Environment[key] = value;
    using var process = Process.Start(start) ?? throw new InvalidOperationException("Unable to start " + executable);
    var output = new StringBuilder();
    async Task Pump(StreamReader reader, bool stderr)
    {
        while (await reader.ReadLineAsync()is { } line)
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

    var stdout = Pump(process.StandardOutput, false);
    var stderr = Pump(process.StandardError, true);
    using var limit = new CancellationTokenSource();
    if (!unlimited)
        limit.CancelAfter(TimeSpan.FromMinutes(10));
    using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation, limit.Token);
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
        if (!cancellation.IsCancellationRequested)
            throw new InvalidOperationException(executable + " exceeded its time limit.");
        throw;
    }

    await Task.WhenAll(stdout, stderr);
    if (process.ExitCode != 0)
        throw new InvalidOperationException(executable + " exited with code " + process.ExitCode + ".");
    return output.ToString();
}

static string FindRoot()
{
    for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        if (File.Exists(Path.Combine(directory.FullName, ".mpcore", "template-manifest.json")))
            return directory.FullName;
    throw new InvalidOperationException("Run from a Fleet Management source checkout.");
}
