using System.Security.Cryptography;
using System.Text.Json;

// Development-only configuration. The mounted volume is private to this Compose project.
var directory = Environment.GetEnvironmentVariable("FLEET_BOOTSTRAP_DIRECTORY") ?? "/run/fleet";
Directory.CreateDirectory(directory);
var statePath = Path.Combine(directory, "secrets.json");
var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
try
{
    var state = File.Exists(statePath)
        ? JsonSerializer.Deserialize<LocalSecrets>(File.ReadAllText(statePath))
        : CreateSecrets();
    if (state is null || !state.IsValid())
        throw new InvalidOperationException("Invalid saved configuration.");

    // Save the source of truth first: an interrupted first boot reuses its credentials.
    if (!File.Exists(statePath)) WritePrivateFile(statePath, JsonSerializer.Serialize(state, jsonOptions));
    WritePrivateFile(Path.Combine(directory, "postgres-user"), state.PostgresUser);
    WritePrivateFile(Path.Combine(directory, "postgres-password"), state.PostgresPassword);
    WritePrivateFile(Path.Combine(directory, "redis-password"), state.RedisPassword);
    var configuration = new
    {
        ConnectionStrings = new
        {
            PostgreSql = $"Host=postgres;Database=fleet_management;Username={state.PostgresUser}_runtime;Password={state.RuntimePassword}",
            MigrationPostgreSql = $"Host=postgres;Database=fleet_management;Username={state.PostgresUser};Password={state.PostgresPassword}",
            Redis = $"redis:6379,password={state.RedisPassword},abortConnect=false"
        }
    };
    WritePrivateFile(Path.Combine(directory, "appsettings.local.json"), JsonSerializer.Serialize(configuration, jsonOptions));
    Console.WriteLine("Development configuration ready; existing credentials retained, values not printed.");
}
catch
{
    // Never expose a parse error containing secret JSON or a connection string.
    Console.Error.WriteLine("Development configuration could not be initialized. Check the configuration volume; saved credentials were not replaced.");
    return 1;
}
return 0;

LocalSecrets CreateSecrets()
{
    var suppliedUser = OptionalVariable("FLEET_POSTGRES_USER");
    var suppliedPassword = OptionalVariable("FLEET_POSTGRES_PASSWORD");
    var suppliedRedis = OptionalVariable("FLEET_REDIS_PASSWORD");
    var suppliedRuntime = OptionalVariable("FLEET_RUNTIME_POSTGRES_PASSWORD");
    var supplied = new[] { suppliedUser, suppliedPassword, suppliedRedis, suppliedRuntime };
    if (supplied.Any(value => !string.IsNullOrEmpty(value)) && supplied.Any(string.IsNullOrEmpty))
        throw new InvalidOperationException("Existing environment import requires all four values.");
    return new LocalSecrets(suppliedUser ?? "fleet_" + Guid.NewGuid().ToString("N")[..12],
        suppliedPassword ?? RandomSecret(), suppliedRedis ?? RandomSecret(), suppliedRuntime ?? RandomSecret());
}

static string RandomSecret() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
static string? OptionalVariable(string name) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : null;

static void WritePrivateFile(string path, string content)
{
    var temporary = path + ".tmp";
    var options = new FileStreamOptions
    {
        Mode = FileMode.Create,
        Access = FileAccess.Write
    };
    if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
    using (var stream = new FileStream(temporary, options))
    using (var writer = new StreamWriter(stream)) writer.Write(content);
    if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(temporary, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    File.Move(temporary, path, overwrite: true);
}

internal sealed record LocalSecrets(string PostgresUser, string PostgresPassword, string RedisPassword, string RuntimePassword)
{
    public bool IsValid() => !string.IsNullOrWhiteSpace(PostgresUser)
        && PostgresUser.All(character => char.IsAsciiLetterOrDigit(character) || character == '_')
        && PostgresUser.Length <= 55
        && new[] { PostgresPassword, RedisPassword, RuntimePassword }.All(value =>
            !string.IsNullOrWhiteSpace(value) && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '+' or '/' or '='));
}
