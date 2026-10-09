# D22 — full-suite verification without silent skips

Date: 2026-10-09. Review items: M1 and M7.

## Default execution

`dotnet run --project tools/VerifyTests --configuration Release` builds current
source with the existing Docker builder and normal layer/NuGet caching. Every run
uses a unique Compose project, new database/configuration/identity volumes,
automatic loopback ports and a separate TRX results directory. It waits for
bootstrap, migrations and application readiness before invoking the full suite.
Credentials enter the child test process in memory and are not printed.

Caching build inputs does not reuse database rows, test outcomes or a previously
tagged application image. Docker evaluates the current source, while the unique
project image names and `up --no-build` bind execution to that build. The runner
rejects an existing project and fails on any skipped, failed or incomplete report.
Cleanup removes only that run's containers, volumes and service images; it never
prunes the shared builder or its cache.

## Optional cold build

Append `-- --cold-build` to request a dedicated empty docker-container builder.
Only this optional mode requires Buildx 0.14 or later. Its builder is removed
afterward. Cold registry downloads can be slow; they are not necessary to prove
fresh database migrations or the business acceptance tests. Build timeout remains
30 minutes. Docker Compose 2.24.4 or later is required in both modes.

This replaces the earlier default of creating an empty builder on every run.
The previous attempted cold runs remain historical evidence of cancelled
downloads, not successful verification.

## Missing dependencies

The integration test project checks its two fixture environment variables before
the standard MSBuild `VSTest` target. Without them, `dotnet test` fails explicitly
with `FLEETTEST001` and points to the provisioning runners. It no longer returns
a green partial solution run just because the dependency tests were skipped.
The xUnit attribute guidance remains for tools that bypass that MSBuild target;
the official runner additionally validates every individual TRX outcome.

Observed negative control: invoking the integration project with `--no-build
--no-restore` and both fixture variables removed returned exit code 1 with
`FLEETTEST001`. Configured full-suite execution is recorded in
[the current verification report](../verification-current-review-fa.md).
