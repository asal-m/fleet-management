using Microsoft.EntityFrameworkCore;
using Npgsql;
using FleetCompany.FleetManagement.Infrastructure.Persistence;

namespace FleetCompany.FleetManagement.Api.Hosting;
// Local Compose provisioning only. Production migrations run under a separately managed owner.
public static class LocalDatabaseBootstrap
{
    public static async Task RunAsync(WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Database:ApplyMigrations"))
            return;
        if (!app.Environment.IsDevelopment())
            throw new InvalidOperationException("Automatic database provisioning is Development-only.");
        var migrationConnection = app.Configuration.GetConnectionString("MigrationPostgreSql") ?? throw new InvalidOperationException("A separate migration connection is required.");
        var runtime = new NpgsqlConnectionStringBuilder(app.Configuration.GetConnectionString("PostgreSql"));
        var owner = new NpgsqlConnectionStringBuilder(migrationConnection);
        if (runtime.Username == owner.Username || string.IsNullOrEmpty(runtime.Password))
            throw new InvalidOperationException("Runtime and migration database accounts must be different.");
        await using var scope = app.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.SetConnectionString(migrationConnection);
        await context.Database.MigrateAsync();
        await using var connection = new NpgsqlConnection(migrationConnection);
        await connection.OpenAsync();
        // Password is a bound parameter, never embedded in SQL, configuration files or log output.
        const string provisionFunction = """
            CREATE SCHEMA IF NOT EXISTS wolverine;
            CREATE FUNCTION pg_temp.provision_fleet_runtime(role_name text, role_password text)
            RETURNS void LANGUAGE plpgsql AS $$
            BEGIN
              IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = role_name) THEN
                EXECUTE format('CREATE ROLE %I LOGIN PASSWORD %L', role_name, role_password);
              ELSE
                EXECUTE format('ALTER ROLE %I LOGIN PASSWORD %L', role_name, role_password);
              END IF;
              EXECUTE format('GRANT USAGE ON SCHEMA fleet, drivers, operations, audit, public, wolverine TO %I', role_name);
              EXECUTE format('GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA fleet, drivers, operations TO %I', role_name);
              EXECUTE format('GRANT SELECT, UPDATE ON public.fleet_availability_revision TO %I', role_name);
              EXECUTE format('GRANT SELECT ON public."__EFMigrationsHistory" TO %I', role_name);
              EXECUTE format('GRANT SELECT, INSERT ON ALL TABLES IN SCHEMA audit TO %I', role_name);
              EXECUTE format('REVOKE UPDATE, DELETE, TRUNCATE ON ALL TABLES IN SCHEMA audit FROM %I', role_name);
              EXECUTE format('GRANT CREATE ON SCHEMA wolverine TO %I', role_name);
              EXECUTE format('GRANT ALL ON ALL TABLES IN SCHEMA wolverine TO %I', role_name);
              EXECUTE format('GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA wolverine, audit TO %I', role_name);
            END $$;
            """;
        await using (var create = new NpgsqlCommand(provisionFunction, connection))
            await create.ExecuteNonQueryAsync();
        await using var provision = new NpgsqlCommand("SELECT pg_temp.provision_fleet_runtime(@role, @password)", connection);
        provision.Parameters.AddWithValue("role", runtime.Username!);
        provision.Parameters.AddWithValue("password", runtime.Password!);
        await provision.ExecuteNonQueryAsync();
    }
}
