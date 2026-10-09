# AI-assisted development notes

## Current review corrections (2026-10-09)

The completion pass added an explicit FLEETTEST001 failure before ordinary
integration `dotnet test` when fixture dependencies are absent. It made normal
Docker caching the full-suite runner default, preserving new databases and
project-specific builds; an independent cold builder is available explicitly.
See [D22](decisions/D22-full-suite-runner.md). Remaining fully qualified names were
replaced with imports/aliases, and Git line endings are declared in .gitattributes.
An initial ResultFailureException name collision was caught by build and fixed
with an explicit alias. The final native-host run passed 179 tests without skips.

Codex replaced the global reservation lock with transaction-scoped mission and
resource locks in a fixed order; unrelated resources can progress independently.
The database revision protocol, eligibility recheck, Audit and unique reservation
indexes remain. REST business states now use JsonStringEnumConverter; numeric
request values remain accepted. Response consumers, gRPC parity tests, SmokeClient
and Postman expectations were updated. Source formatting preserves code tokens;
async endpoint expression bodies were expanded into explicit return blocks.
See [D21](decisions/D21-resource-locks-and-readable-rest-states.md) and
[current verification](verification-current-review-fa.md).

The user selected extensible vehicle/qualification codes with documented limits.
The optional interval reservation policy awaits its business-rule clarification;
no durations or valid-code catalog were invented. The original commit-history
explanation and the framework-owned storage interpretation remain external items.

## Review remediation, step 5 (2026-10-09)

Codex reduced availability revision changes to actual vehicle eligibility or active
reservation changes, and removed vehicle coordination from fresh driver registration.
Audit, transaction ownership, reservation locks and unique indexes are preserved.
Two new regressions failed against the previous code; 160 tests passed after the fix
with no failures or skips. A direct EF command counter replaced incomplete Npgsql
span counts; a nullable override compile failure was fixed before measurement.
Local before/after results show 40 versus 20 EF commands for ten reads after driver
registration, while warm-read latency increased in the later sample. No universal
latency or production throughput improvement is claimed. Current product/test source
ran in test hosts with real fresh dependency volumes; production Docker images were
not rebuilt. See [step 5 evidence](verification-step-5-fa.md) and
[D18](decisions/D18-availability-revision-and-coordination.md).

## Review remediation, step 4 (2026-10-08)

Codex strengthened TRX validation by checking unique test definitions and individual
Passed outcomes against the counters and expected assemblies. Twelve regression
cases exercise valid, corrupt and incomplete reports using the real validator.
Each verification run now owns a docker-container builder and removes its generated
service images and build cache independently during cleanup. Shared builders and
development resources are preserved. Cold builds require network access; the build
timeout is 30 minutes and the workflow timeout is 45 minutes.
M3's existing exclusive reservation and planned-time policy is documented in
[D17](decisions/D17-reservation-and-schedule-policy.md), with a real REST acceptance
test for a future mission. No product business policy or framework source changed.
Observed results and limitations are recorded in [step 4 evidence](verification-step-4-fa.md).

## Review remediation, step 3 (2026-10-08)

Codex replaced blanket Conflict classification in the two business workflows
with module-owned rule classification: missing assignment resources are NotFound,
reservation conflicts remain Conflict and other domain rules are BusinessRule.
Error identities, message descriptors and rejected audit recording are preserved.
The revised assignment regression failed in seven cases against the old mapping.
REST regressions cover transaction state and audit; a test-only gRPC host checks
the installed adapter and rich ErrorInfo. MP Core 0.9.3 maps Conflict to Aborted,
and BusinessRule to FailedPrecondition. No product gRPC command was added.
See [step 3 evidence](verification-step-3-fa.md) for observed final verification
and its limits. A single Postman lifecycle expectation was updated; its existing
untracked files remain outside the staged submission.

## Review remediation, step 2 (2026-10-08)

Codex implemented the .NET full-suite runner and Ubuntu verification workflow,
updated the integration skip guidance and recorded the observed verification.
The runner does not depend on PowerShell. Windows execution passed all 135 tests
with zero skips using fresh Docker resources. Negative controls rejected skipped
and missing reports and an unavailable Docker executable. A Linux container on
the Windows Docker Desktop host failed to reach published loopback ports; its
failure and cleanup are recorded, not presented as a successful Linux test run.
The GitHub workflow and macOS execution have not been observed.
See [step 2 evidence](verification-step-2-fa.md). No authorship claim or business
policy was changed as part of this step.

## Tool, scope and developer involvement

The developer confirmed on 2026-10-05 that this project was developed with
Codex's help. Codex was used for domain modeling, bounded-context planning,
vertical slices, REST/gRPC contracts, security, audit, caching, tests and review.
The developer supplied and approved business decisions and requested corrections
after the challenge coverage review. See [architecture.md](architecture.md).

## What was generated

Codex generated aggregates/value objects and named rules for Vehicle, Driver and
Mission; repository/read-model ports and EF adapters; validators and workflow
handlers; thin REST endpoints and four gRPC queries; audit policies and actions;
availability caching/concurrency coordination; tests, Compose and documentation.
EF tooling generated the database migrations, which were inspected before use.
The latest review added ComposeBootstrap, Development-only file configuration,
Test-ComposeEnvironment.ps1 and a sourced decision record for framework storage.

## Human/manual changes

The work was done with Codex's assistance. No independent manual source edits
have been reported by the developer; none are claimed here. The recorded human
contribution consists of business decisions, scope approval and review feedback.
The corrections below were applied by Codex under that direction. Manual edits
and independent human code review should be recorded here if performed later.

## Suggestions rejected or corrected

| Suggested approach | Review finding and final choice |
|---|---|
| A Mission's own optimistic version prevents shared-resource races | Two different Missions have different versions. Assignment and maintenance now use a shared PostgreSQL transaction lock; partial unique indexes guard resource ownership. |
| Remove one availability cache key after a write | Another instance can retain a local entry or refill a stale key. The cache uses a database revision incremented in the business transaction, a 30-second TTL and a revision recheck. Assignment reads trusted database state. |
| Keep eligibility checks only in workflow orchestration | Domain rules now enforce capacity, qualification, activity, maintenance and reservations before mutation. Application supplies trusted facts through Contracts. |
| Use dotnet user-jwts for local authentication | The framework excludes symmetric signatures. A separate Development-only OIDC/JWKS fixture issues RSA-signed tokens; the backend retains real bearer validation. |
| Accept default record ToString logging | It exposes names, plates and locations. Sensitive commands log only their type names, protected by a regression test. |
| Use the migration owner as the application database account | It permits alteration of audit history. Compose provisions a separate runtime role with Audit SELECT/INSERT permissions. |
| Treat preconfigured Compose startup as a fresh-clone check | Required shell variables made direct up fail. A one-shot bootstrap now persists random credentials in a private Docker volume and supports one-time import for existing installations. |
| Delete Wolverine storage to close the Outbox/Inbox exclusion | MP Core 0.9.3 registers it as part of its execution foundation. Deleting tables or patching package code would hide the incompatibility and risk transaction guarantees. D16 remains explicit and sourced. |

Other corrections include PostgreSQL timestamp precision, explicit JwtSecurityToken
construction caught by a Docker build, and asynchronous TestServer cleanup with
console logging instead of Windows EventLog handles. Assertions and authorization
were preserved throughout the corrections.

## Why the final design was chosen

Each module owns its data and exposes Contracts. Named rules protect aggregates;
transports call shared Application behavior. MP Core owns transaction commit and
failure/security/audit infrastructure. A global resource lock favors a simple,
verifiable consistency model at the cost of write throughput. Availability is a
cached snapshot, not permission to assign. Runtime configuration and credentials
stay outside source control.

## Verification and remaining limits

Verification results, including the latest run, are in
[verification.md](verification.md). Tests exercise real PostgreSQL/Redis,
asymmetric JWT validation, eight competing assignments, cache changes across
hosts, successful/rejected audit and REST/gRPC parity. Socket smoke verification
uses actual local OIDC discovery and the four gRPC operations.

Generated code was corrected after inspection and execution; these checks are
not evidence of independent human source review. The developer should review
and be able to explain the decisions before submission.
[D16](decisions/D16-framework-message-storage.md) still requires the evaluator's
interpretation or an officially supported framework option. No blanket claim of
challenge acceptance is made.

Earlier stage-by-stage notes remain local historical material. The submission's
current evidence and limitations are documented in verification.md and the linked
runtime review; they do not imply that later documentation edits reran the suite.
