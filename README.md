# Fleet Operations — MP Core 0.9.3

A .NET 10 Modular Monolith with Fleet, Drivers, Operations and Administration.
REST and gRPC invoke the same Application queries. PostgreSQL owns business data
and audit; MP Core Hybrid Cache combines local memory and Redis.

## Run with Docker

Prerequisite: Docker Compose with Linux containers. Run in this directory:

```powershell
docker compose up --build
```

No host .NET SDK, EF CLI, User Secrets, .env file or setup script is required for
a fresh installation. A one-shot bootstrap generates random owner/runtime/Redis
credentials into a private Docker volume. PostgreSQL reads password files; the
application reads its generated Development-only configuration. Files are mode
0600, and the application/Redis/bootstrap run as UID 1654. Migration and runtime
database identities remain distinct. Redis is an ephemeral cache, with snapshots
and append-only persistence disabled. Credentials survive container recreation.
Use `docker compose up --build -d` for background execution; `docker compose down`
stops the environment while retaining database, signing-key and configuration volumes.
Keep the database and configuration volumes together when backing up or restoring.

**Existing installation from the former User Secrets workflow:** run
`.\scripts\Initialize-ComposeEnvironment.ps1` once in the terminal before the first
up with this version. Bootstrap imports the existing credentials only when its
configuration volume is empty; later up commands need no initialization.
This preserves the existing PostgreSQL data and runtime account. A new configuration
volume cannot discover the password of an existing database volume.

Compose starts the application, PostgreSQL, Redis and a small **Development-only**
OIDC metadata/JWKS/token fixture. The fixture is separate from the bearer-only
backend and supports no user accounts, passwords or production login.
It is necessary to verify real RSA signatures and OIDC discovery without adding
Keycloak. Production must use a managed OIDC provider and HTTPS metadata.

| Surface | Local access |
|---|---|
| REST | http://localhost:8080 |
| REST OpenAPI UI | http://localhost:8080/openapi-ui |
| OpenAPI document | http://localhost:8080/openapi/v1.json |
| gRPC HTTP/2, reflection | localhost:8081 |
| Readiness / liveness | /health/ready /health/live on REST |
| Development token fixture | http://localhost:55480 |
| PostgreSQL / Redis | localhost:55432 / localhost:56379 |

Only loopback host ports are published. Production does not enable anonymous
description surfaces by default. Metrics `/metrics` requires a bearer token.

## Configuration and migrations

Manifest: [.mpcore/template-manifest.json](.mpcore/template-manifest.json), pinned
to CLI/template/framework 0.9.3; transport both, modular-monolith, hybrid cache,
PostgreSQL audit, messaging none, timeseries none, AI tooling codex.

Required runtime configuration: `Security:Authority`, `Security:Audiences:0`,
`ConnectionStrings:PostgreSql`, `ConnectionStrings:Redis`. Use environment
variables (double underscore separators) or User Secrets, never source files.
The generated appsettings placeholders are not usable credentials.
Compose supplies connection settings from its private mounted file. The file is
accepted only in Development; explicit environment/command-line settings override it.

Local Compose also sets `ConnectionStrings:MigrationPostgreSql` and
`Database:ApplyMigrations=true`. Development bootstrap migrates with an owner
account and provisions a distinct runtime role. Runtime Audit permissions are
SELECT/INSERT plus identity-sequence usage; UPDATE/DELETE/TRUNCATE are revoked.
The runtime account cannot migrate business schemas. Production runs migrations
separately, sets automatic provisioning false, and grants equivalent runtime
permissions through deployment administration.

Local Compose applies migrations on startup. To run the API on the host with
separate Docker dependencies, install .NET 10 and Compose 2.24.4 or later:

```sh
dotnet run --project tools/LocalDevelopment --configuration Release -- run
```

This uses `fleetmanagement-dependencies` with its own database/configuration/key
volumes, PostgreSQL 56432, Redis 57379 and identity 56480. The native host listens
on loopback REST 8180 and gRPC 8181. It builds before starting, applies migrations
using the owner account and runs with the restricted runtime account. OIDC discovery
and RSA token validation remain enabled. Configuration is passed in child process
environment; no User Secrets, EF CLI or credential file in the checkout is needed.
The absence of appsettings.Development.json is deliberate: this tool supplies the
Development settings while appsettings.json remains the explicit production template.

Use `-- verify` instead of `-- run` for live socket smoke and the full test suite
with strict TRX validation. Verification creates fictional demo records/Audits in
this development database and stops its native host afterwards. Dependencies and
data remain until `-- down`, which stops containers while retaining all volumes.
Ctrl+C also stops the native host and retains dependencies. Do not verify against
a shared development host already running on the same ports.
`scripts/Start-LocalDependencies.ps1` is an optional wrapper around this command;
`-RunTests` selects verify. Its former PostgreSqlOnly option is rejected explicitly
because the complete workflow requires Redis and the identity fixture.
See [local development](docs/local-development.md) for custom ports, old-data
migration and the verified Windows/Linux-container runs. Native Ubuntu CI and macOS
execution are tracked separately in the current verification report.
Fresh startup and restart on Windows each passed 172 tests without failures/skips;
real socket smoke and simultaneous environment isolation passed. Initial failures
and the final cleanup evidence are recorded in [step 7](docs/verification-step-7-fa.md).

Migrations live under `src/FleetCompany.FleetManagement.Infrastructure/Persistence/Migrations`.
They cover vehicles/audit, drivers, missions, partial reservation uniqueness and
the technical availability revision row.

## Authentication and roles

Fetch a local token without printing it:

```powershell
$token = (Invoke-RestMethod -Method Post -Uri http://localhost:55480/development/token -ContentType application/json -Body '{"role":"Operator"}').access_token
$headers = @{ Authorization = "Bearer $token" }
Invoke-RestMethod -Uri http://localhost:8080/api/operations/missions/active -Headers $headers
```

Roles are exactly `Operator`, `FleetManager`, `Administrator`.
Tokens last five minutes; renew them when expired. Claims use `realm_access.roles`.
Actor/audit identity comes exclusively from the validated token.
No authentication or authorization bypass is used for local development.

## REST operations

| Method | Path | Required role |
|---|---|---|
| POST | /api/fleet/vehicles/ | FleetManager |
| GET | /api/fleet/vehicles/{id} | FleetManager or Operator |
| GET | /api/fleet/vehicles/available?limit=50 | FleetManager or Operator |
| PUT | /api/fleet/vehicles/{id}/status | FleetManager |
| POST | /api/fleet/vehicles/{id}/maintenance/start | FleetManager |
| POST | /api/fleet/vehicles/{id}/maintenance/complete | FleetManager |
| POST | /api/drivers/ | FleetManager |
| GET | /api/drivers/{id} | FleetManager or Operator |
| GET | /api/drivers/available?limit=50 | FleetManager or Operator |
| POST | /api/operations/missions/ | Operator |
| GET | /api/operations/missions/{id} | Operator |
| GET | /api/operations/missions/active?limit=50 | Operator |
| POST | /api/operations/missions/{id}/schedule | Operator |
| POST | /api/operations/missions/{id}/assign | Operator |
| POST | /api/operations/missions/{id}/start | Operator |
| POST | /api/operations/missions/{id}/complete | Operator |
| POST | /api/operations/missions/{id}/cancel | Operator |
| GET | /api/administration/audit/?entityId={id}&page=1&size=50 | Administrator |

List limits and audit page sizes: 1..200. Audit page must be positive.
Vehicle and Mission creation return 201 with Location pointing to their GET endpoint.
Driver registration returns 201 with the created representation and a Location pointing
to its GET endpoint. Reads/changes return 200. Invalid input returns
400, unauthenticated 401, forbidden 403. GET of a missing Vehicle or Mission returns 404.
Missing Vehicle or Driver references during Assign return 404. Reservation conflicts
return 409; non-conflicting business-rule violations (capacity, eligibility, lifecycle,
schedule and maintenance state) return 422. Error identities and audit attempts are
preserved. MP Core 0.9.3 maps Conflict to gRPC Aborted and BusinessRule to
FailedPrecondition; NotFound and Validation map to NotFound and InvalidArgument.
The current gRPC business surface contains queries only; command-failure mapping is
verified through a test-only service. See [step 3 evidence](docs/verification-step-3-fa.md).
Failures use MP Core Problem Details with stable domain/code and localized messages.

Examples of request bodies:

```json
{"plateNumber":"DEMO-001","typeCode":"TRUCK","capacityKilograms":2000,"baseStatus":1}
{"firstName":"Ali","lastName":"Demo","status":1,"qualifications":["TRUCK"]}
{"origin":"Tehran","destination":"Shiraz","requiredCapacityKilograms":1000}
{"scheduledTime":"2030-01-01T09:00:00Z"}
{"vehicleId":"<vehicle-guid>","driverId":"<driver-guid>"}
{"status":2}
```

REST business-state fields are strings: `Active`, `Inactive`, `UnderMaintenance`,
`Draft`, `Scheduled`, `Assigned`, `InProgress`, `Completed` and `Cancelled`.
Existing numeric request values remain accepted: Active=1/Inactive=2 for vehicle
base and driver statuses. Effective UnderMaintenance is derived through maintenance.
Clients that previously read numeric response states must switch to the named fields.
gRPC proto enum numbers, HTTP status codes and Audit outcome codes are unchanged.
Schedule requires a future ISO timestamp with explicit Z/offset, stored in UTC at
PostgreSQL microsecond precision. Commands with no fields accept an empty object.

## gRPC

Contracts: `src/FleetCompany.FleetManagement.Api/Protos/fleet_vehicles.proto`
and `operations_missions.proto`. Services expose GetVehicle, GetAvailableVehicles,
GetMission and GetActiveMissions. Supply `authorization: Bearer <token>` metadata.
Use plaintext HTTP/2 only locally; production requires TLS.
Failures preserve the same domain/code through rich gRPC ErrorInfo.

A runnable C# client exercises all four methods, REST lifecycle, real OIDC and Audit:

```powershell
dotnet run --project tools/SmokeClient/SmokeClient.csproj --configuration Release
```

It leaves fictional demo records for review: a completed Mission and inactive
Vehicle, with append-only audit history. It never prints tokens.
For runtime Audit privilege verification as well, use
`.\scripts\Verify-ComposeSmoke.ps1`, which reads the runtime connection only in memory.

## Tests and operational verification

The full-suite runner is written for Windows, Linux and macOS; its execution and
isolation policy is described in [D22](docs/decisions/D22-full-suite-runner.md).
Install the .NET 10 SDK
and Docker with Linux containers (Compose 2.24.4 or later), then run:

```text
dotnet run --project tools/VerifyTests/VerifyTests.csproj --configuration Release
```

The C# runner builds a fresh isolated Compose project with random loopback ports,
waits for automatic bootstrap/migrations and readiness, and supplies PostgreSQL/Redis
fixture settings to the test process in memory. No User Secrets, manual migration
or environment-variable setup is required. It reads TRX results from all three test
projects and matches counters to unique individual Passed results and their test
definitions; skipped, failed, missing or incomplete results fail verification. Results remain
under `.scratch/full-suite/<run-id>/results`; only that run's test containers and
volumes and generated service images are removed, including after failure or Ctrl+C.
The default build uses Docker's normal layer/NuGet cache and builds current source
into project-specific images. Databases, volumes and test results are always fresh.
For an optional build with an independent empty cache, append `-- --cold-build`;
this mode requires Buildx 0.14 or later and removes its dedicated builder afterward.
Both modes preserve shared builders and caches. Initial downloads require
registry/NuGet access and free disk space; cleanup requires a responsive Docker daemon.
Builds have a 30-minute timeout; other commands have 10 minutes and each cleanup
command has 2 minutes. The CI job allows 45 minutes.
The development environment
stays separate. The prepared Linux CI workflow invokes the same command and retains
the TRX artifacts. Current Windows and Linux SDK-container suites each passed 179
tests with zero failures/skips; a current product Docker image also passed socket
smoke checks. See [current evidence](docs/verification-current-review-fa.md).
Cold builds with the dedicated builder have not completed because downloads were
very slow. Native Ubuntu CI and macOS execution have not yet been observed.
See [step 6 evidence](docs/verification-step-6-fa.md), [step 5 evidence](docs/verification-step-5-fa.md), [step 4 evidence](docs/verification-step-4-fa.md) and the historical
[step 2 evidence](docs/verification-step-2-fa.md).

The full-suite command runs automated tests. For the separate live socket smoke
and Redis outage checks against the development environment, use the scripts below.
To run automated tests against an already-running development Compose environment:

```powershell
.\scripts\Test-ComposeEnvironment.ps1
```

After Compose starts, this reads the generated configuration in memory and executes
`dotnet test` with real PostgreSQL and Redis. It requires the .NET 10 SDK and
PowerShell on the host; no EF CLI or host User Secrets are needed. Credentials
are not printed and process environment overrides are restored afterwards.
Running `dotnet test`
without the fixture connection environment fails with `FLEETTEST001` before the
integration suite runs. Tests use generated RSA keys,
actual JWT verification, unique fictional IDs and cleanup only their own business
rows. Audit records are never deleted by test code.

To exercise real cache failure and recovery after Compose has started:

```powershell
.\scripts\Verify-RedisFailure.ps1
```

The script stops only this project's Redis and restarts it in a finally block.
Do not run it concurrently with the automated suite.

To repeat verification from empty database/configuration volumes:

```powershell
.\scripts\Verify-FreshCompose.ps1
```

This builds a separate Compose project on ports 58080/58081/58480/58432/58379,
waits for automatic bootstrap and migrations, runs the full suite and live
REST/gRPC/OIDC/Audit checks, then stops and restarts that project's Redis to
verify fallback and recovery. It removes only its temporary containers/volumes
in a finally block. The existing local environment stays running. Requires
Docker Compose 2.24.4 or later (`!override`), .NET 10 SDK and PowerShell.
Port parameters can be supplied when the default verification ports are occupied.

The Application test project includes architecture guards for module project
references, forbidden EF/ASP.NET/broker dependencies in Domain/Application, and
English/Persian text for every business rule, validation and failure message key.
These guards inspect source with Roslyn and require the source checkout.

The legacy PowerShell test and verification scripts require Windows: they resolve
`dotnet.exe` / `docker.exe`, and the Redis script also checks a Windows Docker path.
Dependency tests require `FLEET_TEST_POSTGRES_CONNECTION` and
`FLEET_TEST_REDIS_CONNECTION`; `Test-ComposeEnvironment.ps1` sets them in memory.
The cross-platform C# full-suite command above sets them independently. A bare
`dotnet test` without fixture settings fails with `FLEETTEST001`; use the full-suite
runner to provision isolated dependencies and run all tests.

Request logs contain trace/span scopes; `X-Trace-Id` supports correlation.
The trace test follows Transport, Application, Domain, database and cache spans.

## Design and review

- [Architecture and decisions](docs/architecture.md)
- [Exclusive reservation and planned-time policy (M3)](docs/decisions/D17-reservation-and-schedule-policy.md)
- [Step 4 verification and cleanup evidence](docs/verification-step-4-fa.md)
- [Availability cache revision and coordination (M4)](docs/decisions/D18-availability-revision-and-coordination.md)
- [Step 5 tests and before/after measurements](docs/verification-step-5-fa.md)
- [Step 6 validation, mapping and architecture verification](docs/verification-step-6-fa.md)
- [Module ownership and read contracts (M5)](docs/decisions/D19-module-ownership-and-read-contracts.md)
- [Mission lifecycle and extensible codes](docs/decisions/D20-lifecycle-and-extensible-codes.md)
- [Confirmed authorship and open history questions](docs/authorship-fa.md)
- [Verification and challenge coverage](docs/verification.md)
- [AI development notes](docs/ai-development-notes.md)
- [Maintenance and mission policy](docs/architecture.md#سیاست-تعمیر-و-مأموریت)
- [Generated MP Core development workflow](docs/development-workflow.md)
- [Review remediation progress](docs/remediation-status.md)

**Known open decision D16:** MP Core's generated Wolverine foundation creates
incoming/outgoing-envelope storage even with messaging none. No broker,
Publish/Send workflow or product Outbox/Inbox implementation was introduced.
The challenge excludes Outbox/Inbox, so strict acceptance still needs evaluator
clarification or an officially supported framework configuration. Framework
source is unchanged. This repository does not claim that D16 has been resolved.
The exact API/source evidence and rejected alternatives are recorded in
[D16](docs/decisions/D16-framework-message-storage.md).
