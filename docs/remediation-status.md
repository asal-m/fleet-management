# Review remediation status

Review: 4_Review_Asal.pdf, 7 October 2026, score 69/100, resubmission required.
This record describes changes to the local submission. Acceptance belongs to the evaluator.

## Current status (9 October 2026)

The completion pass verified both the native-host runner and the standard
full-suite Compose runner: each passed 179 tests with zero failures/skips.
The latter built current product source and performed bootstrap/migrations on
fresh volumes. Plain integration `dotnet test` without fixture settings now
fails explicitly with FLEETTEST001. Normal Docker build caching is the default;
an independent cold builder is opt-in. See [D22](decisions/D22-full-suite-runner.md)
and the completion section in the current evidence report. Remaining imports and
Git line-ending declarations were corrected. No commit/push was performed.

Current working-tree verification passed 179 tests on Windows and 179 inside a
Linux SDK container, with zero failures/skips. Current product Docker build,
socket smoke and live metrics/trace export passed. Resource-scoped locks replace
the global write lock; REST business states use named enum values. Commit-time
reservation conflicts, including rollback and HTTP 409, were exercised. Code
formatting and README/consumer contracts were corrected. See
[current evidence](verification-current-review-fa.md) and
[D21](decisions/D21-resource-locks-and-readable-rest-states.md).

The sections below are historical step reports. Their "next item" statements
are not the current backlog. Interval reservation business rules await the user's
answer; extensible type codes remain by the user's choice. M6 history explanation,
D16 evaluator interpretation, native GitHub/Linux orchestration and macOS evidence
remain external or pending. No commit/push was performed.

## Step 1: restore the documentation deliverables

Date: 8 October 2026. Scope: C1 and documentation for C2.

- Remove the blanket Markdown/documentation exclusions from .gitignore; keep precise
  exclusions for personal training material, local history, secrets and generated output.
- Include README, architecture, AI notes, verification, current runtime evidence,
  the generated development guides, AGENTS.md, module guidance and the ten canonical
  Codex procedures with their adapters.
- Document maintenance with Assigned/InProgress/Scheduled missions, released reservations
  and the assignment-vs-maintenance race, including the existing policy and alternatives.
- State the actual workflow error mapping and Windows-only runner limitations.
- Resolve local documentation links against the files selected for the submission.

No application code or business policy changed. No product tests were rerun for this
documentation-only step. The 135 passing tests recorded on 8 October apply to the
earlier runtime verification, not a fresh execution during this step.

Submission checks: the 40 selected files are in the Git index, including all five
required documentation entry points and the ten canonical/adaptor skill pairs.
The 37 Markdown files have no missing or Git-ignored local link targets. Training
and local-history exclusions still apply; existing untracked Postman work was not staged.

C1 is locally prepared once these files are staged; it reaches the evaluator only
after commit and publication of the corrected submission. C2's policy is now written;
the existing tests remain its runtime evidence. User confirmation of authorship (M6)
and the other technical review issues remain open.

## Step 2: one full-suite command without hidden skips

Date: 8 October 2026. Scope: M1 implementation and test-environment isolation.

- Add a .NET console runner, independent of PowerShell, which starts a fresh Compose
  project with generated names, empty volumes and automatic loopback host ports.
- Let the existing bootstrap supply credentials and apply migrations. Pass test
  connection settings in the child process environment; do not print credentials.
- Run all three test projects and inspect their TRX reports. Reject skipped tests,
  failed tests, empty reports, duplicate projects and missing project reports.
- Remove only this generated environment, also after a failure or Ctrl+C.
- Add an Ubuntu GitHub Actions workflow using the same command and retaining TRX
  artifacts. This workflow has been prepared locally, not executed on GitHub.
- Update the integration skip messages to direct developers to the full-suite command.
  Plain `dotnet test` without dependencies still skips integration tests; it is not
  the full verification entry point.

Command from the repository root:

```sh
dotnet run --project tools/VerifyTests/VerifyTests.csproj --configuration Release
```

Requires .NET 10 SDK, Docker using Linux containers, and Compose 2.24.4 or later.
Windows execution passed 91 Domain, 31 Application and 13 Integration tests: 135
passed, zero skipped. Negative controls rejected a skipped-test report, a missing
Integration report and an unavailable Docker executable. The runner builds with
zero warnings and errors. Details: [step 2 evidence](verification-step-2-fa.md).
The user-requested repeat (run `5cbd5b10050e445183be86f76600629a`) also passed
all 135 tests with zero skips. Direct inspection confirmed every individual TRX
outcome was Passed, with all three project totals matching their result counts.

A Linux SDK container on this Windows Docker Desktop host successfully compiled
the runner and started the fresh dependencies, but could not reach Windows loopback
published ports. Its readiness check failed, returned a nonzero exit code and removed
its own environment. This is not a passing Linux full-suite execution. Native Linux
CI execution and macOS execution remain unverified; M1's cross-platform acceptance
remains pending the Linux CI result. Existing development readiness remains healthy.

This isolates verification resources only. The normal developer Compose project
still has its original name and fixed ports; general M7 remediation remains open.
Legacy PowerShell API checks remain optional Windows tools. No business behavior,
authentication rules or framework package changed in this step.

## Step 3: classify failures by their meaning

Date: 8 October 2026. Scope: M2.

- Replace blanket Conflict conversion in MissionWorkflow and VehicleWorkflow with
  rule classification owned by each module's Application failure factory.
- Missing assignment resources are NotFound; vehicle/driver reservation conflicts
  and attempts to maintain/deactivate a reserved vehicle remain Conflict. Other
  domain rule failures are BusinessRule. Identity and localizable message are preserved.
- Retain rejected-attempt audit recording and the failure/rollback path.
- Update Application and REST contracts, and add missing-resource, maintenance and
  past-schedule regressions with persisted state and audit checks.
- Verify the installed MP Core 0.9.3 gRPC adapter with a test-only service:
  Conflict maps to Aborted, BusinessRule to FailedPrecondition, Validation to
  InvalidArgument and NotFound to NotFound; rich ErrorInfo retains domain/code.
- Update the README and a Postman lifecycle expectation. Previously untracked
  Postman work remains untracked; Newman was not run. The SmokeClient expectation
  for maintenance of a reserved vehicle remains correctly 409.

Before the implementation change, nine assignment-category regression cases ran:
seven failed against the old mapper and the two reservation conflicts passed.
After the fix, all 33 Application tests and the five standalone gRPC adapter cases
passed without skips. Full-suite evidence: [step 3 report](verification-step-3-fa.md).
The final fresh-environment run (`0a08c19f903146b08f3fceb8bae250b6`) passed 91
Domain, 33 Application and 20 Integration cases: 144 passed, zero failed/skipped.
Direct TRX inspection confirmed every individual outcome was Passed. Bootstrap,
migrations, readiness and cleanup succeeded. The corrected development application
was rebuilt and its readiness is Healthy. M2 is implemented and verified locally;
the changes are not yet committed or published.

An initial full-suite attempt (`7707b144e1534197953d3b15bfebcef2`) was interrupted
by a Docker BuildKit EOF. Docker logs reported disk-full and its engine stopped;
this run failed and its automatic cleanup could not contact Docker. After recovery,
no containers or volumes existed for that interrupted run. Temporary probe build
output created during this step was removed; the stopped engine was restarted and
the development containers were restored using their existing volumes.

## Audit of steps 1–3 (8 October 2026)

The audit re-read all original TRX outcomes: 144 Passed, zero failed/skipped.
Documentation links and the staged diff checks passed. No fresh full-suite run
was performed during this audit. Two follow-ups were found in step 2:

- A1: the report validator trusts Counters without matching individual result
  counts/outcomes. Removing one UnitTestResult from a copy still returned success.
  Original reports remain complete and all actual outcomes were Passed.
- A2: cleanup removes containers/volumes but leaves generated images and build
  cache. Dedicated test-run images were manually cleaned up after the disk-full
  incident; automatic resource management still needs completion.

Native Linux CI acceptance remains pending. Current development containers are
stopped; earlier Healthy evidence is historical. Details and acceptance criteria:
[audit report](audit-steps-1-3-fa.md). Complete A1/A2 before declaring step 2 done.

## Step 4: fix audit findings A1/A2 and document M3

Date: 8 October 2026. TRX validation now matches counters to complete, unique
test definitions and individual Passed outcomes from the expected assembly.
Twelve validator regressions passed; the exact incomplete-report reproduction
from the audit now fails. The original 144-test reports remain valid.

Verification now uses a dedicated docker-container builder per run. Cleanup removes
generated service images with Compose `--rmi local`, then independently removes the
builder and its cache. Shared caches/builders and development resources are preserved.
Cold-build timeout is 30 minutes; the CI job allows 45 minutes.

Two cold-build attempts did not reach tests: one timed out while downloading the SDK,
the other was stopped because downloads remained very slow. Both cleaned up their
isolated builders/cache. A separate fresh-volume run using local step-3 product images
and current host test source passed 91 Domain, 33 Application and 33 Integration cases:
157 passed, zero failed/skipped. Strict TRX validation passed. Generated image tags,
containers, volumes, network and the dedicated nonempty cache were removed after
success. All 16 existing containers, 17 volumes and original builders were preserved;
shared cache remained 146 records / 6.909 GB. See [step 4 report](verification-step-4-fa.md).
Successful cold-build execution with the dedicated builder remains unobserved.

[D17](decisions/D17-reservation-and-schedule-policy.md) documents M3's existing
exclusive reservation and ScheduledTime policy. A real REST regression verified a
future mission's immediate reservation, conflict, early start and resource release.
This is the review's minimal documentation path; evaluator acceptance is pending.
No new time-interval business policy, production code or migration was introduced.

## Step 5: targeted availability invalidation and driver independence (M4)

Completed locally on 9 October 2026, with baseline measurements from 8 October.
Mission revision changes only when active reservation membership changes; Vehicle
revision changes only when operational eligibility changes. Inactive registration
does not invalidate availability. Real business changes still retain Audit.
RegisterDriver no longer takes the vehicle coordination lock or changes its cache
revision. Existing resource/reservation coordination and unique indexes are preserved.
The command impact table and concurrency limits are documented in
[D18](decisions/D18-availability-revision-and-coordination.md).

Two new regressions failed against the old implementation. After the fix, the full
suite passed 91 Domain, 33 Application and 36 Integration cases: 160 passed, zero
failed/skipped. Twenty-six revision checks, eight independent driver registrations
under a held lock, existing resource races and two-host cache checks passed.
For ten availability reads following driver registration, EF commands fell from
40 to 20; warm reads still require two database calls each. Local latency results
are mixed and are not a production SLO. See [step 5 evidence](verification-step-5-fa.md).
Fresh dependency volumes were used; HTTP/gRPC test hosts built the current product
source. Dependency/readiness images remained from step 3. New production Docker
images, cold-build acceptance and Linux CI were not observed in this step.
The global lock's resource throughput limitation and optional finer-lock redesign
remain; this step completes targeted invalidation, not that larger redesign.

## Planned follow-up after step 5

M5: document module data/port ownership and the conceptual dependency cycle, then
review the shared technical kernel without changing business policy. Complete M1 acceptance
when the prepared Linux CI workflow runs on the published submission. Database
uniqueness exception mapping (m6) and missing status robustness (m12) remain open.

## Step 6: ownership, application validation and transport mapping

Date: 9 October 2026. [D19](decisions/D19-module-ownership-and-read-contracts.md)
documents the minimal M5 path: data/port ownership, the conceptual cycle versus
project references and the deliberate shared technical kernel. Architecture guards
passed. [D20](decisions/D20-lifecycle-and-extensible-codes.md) records lifecycle
limits and the current free-code policy; no new catalog or business policy was imposed.

Fleet handlers now choose typed operations; audit strings no longer dispatch
maintenance/status behavior. This completes that part of m1; Mission orchestration
still needs separate handler refactoring. Domain instrumentation moved to Application
(m2) with an architecture guard and preserved trace evidence. Vehicle lookup/query
and both gRPC methods map actual state through a shared mapper (m3). Schedule parsing
and validation live in Application (m4), including direct-handler guards. Direct
vehicle/driver registration returns Validation for missing/unknown status (m12).

The full suite passed **172 cases: 91 Domain, 44 Application, 37 Integration; zero
failed/skipped**. Strict TRX validation passed. Initial new tests caught an invalid
FieldViolation path; this was corrected before the full successful run. REST/gRPC
state parity and invalid schedule without state/Audit mutation passed. See
[step 6 evidence](verification-step-6-fa.md) for changes, test counts and limits.

Fresh dependency volumes were used with step-3 local startup/readiness images;
current HTTP/gRPC source was built in host tests. New product images, cold-build
acceptance and Linux CI remain unobserved. Cleanup completed with exit code 0;
all 16 original containers and 17 original volumes match the earlier snapshot.
The dedicated builder/cache and generated image tags are absent; original
default/desktop-linux builders remain. C: had 13.29 GB free after this run.

The author confirmed **Asal Mozafari**; the account/history explanation remains
open in [authorship](authorship-fa.md). Changes are local; no commit/push occurred.
Next technical work: M7/m11 environment separation and portable host startup,
then m6 commit-conflict mapping. Complete m1, m5/m9/m10 and remaining external
acceptance evidence separately. D16 and M6 remain explicit open items.

## Step 7: isolated dependencies and portable native-host workflow (M7/m11)

Date: 9 October 2026. The dependency Compose project, database/configuration/key
volumes and published ports are separate from the full Docker environment.
LocalDevelopment supplies private child-process settings, owner migrations,
restricted runtime access and the real development OIDC fixture without changing
User Secrets. It requires .NET 10, Docker Linux containers and Compose 2.24.4+;
the optional PowerShell script delegates to it. Legacy data is preserved in its
original volume; the new native workflow uses a separate database.

Fresh startup and data-preserving restart each passed 91 Domain, 44 Application
and 37 Integration cases: **172 passed, zero failed/skipped**, with strict TRX
validation. Real socket smoke passed for REST and all four business gRPC queries,
JWT/roles and runtime Audit privileges. Full/native environments ran together;
credentials/volumes were distinct and unchanged across restart. CLI checks reject
the full project name and duplicate ports before starting resources.

Verification found and corrected Windows DLL build ordering and leakage of
Development-only HTTP metadata into Testing. A failed suite had 155 passed and
17 failed before the environment fix; failures were preserved in the report.
Restart also exposed an unused anonymous Redis volume; an explicit tmpfs fixed
its creation and final cleanup succeeded. All 16 original containers and 17
original volumes remain unchanged; temporary images/resources were removed.
Shared builders/cache were preserved. See [step 7 evidence](verification-step-7-fa.md).

Native-host Ubuntu CI is prepared but unobserved. Windows execution is evidence;
Linux/macOS support remains designed until those jobs/runs pass. Bootstrap/identity
Docker builds used cache; the parallel full environment used step-3 images, so
new product images and cold-build acceptance remain pending. No framework or
business-rule change and no commit/push occurred. Next technical item: m6,
mapping real reservation uniqueness failures at transaction commit to Conflict.
