# Validation and evidence

Status: **current commands plus proposed test requirements**.
Check the project layout at HEAD before running commands.

## Existing commands

From the current repository root in PowerShell:

```powershell
git status --short
git diff --check
dotnet build .\GeminiCapyPet.csproj -c Release
```

There is no committed test project at baseline 8d98105. A successful build or an
empty dotnet test run is not evidence that behavior tests exist or passed.
Future commands must use actual created test-project paths, not the proposed
directory tree copied into a command.

## Documentation-only validation

- Check every local Markdown link resolves, including links from the skill.
- Verify source-linked files and named symbols against the current repository.
- Check proposed types/projects are explicitly labeled as pending.
- Keep roadmap completion claims consistent with source and evidence.
- Review the diff for unrelated edits and run git diff --check.
- A runtime build/restart is unnecessary when only Markdown changed.

## Proposed automated suites

These are required as their corresponding implementation is introduced; they are
not existing tests.

| Area | Meaningful scenarios |
| --- | --- |
| Coordinator | Activate is idempotent; toggle visible hides; hidden/minimized restores |
| Concurrency | A launches then B selected; stale A never steals focus; same-app launch deduplicates |
| Toggle intent | Two rapid toggles while hidden end hidden; cancel during acquisition preserves intent |
| Handoff | B fails and A stays usable; partial handoff recovers observed state |
| Identity | Closed/reused HWND, recycled PID/lifetime, unrelated browser window, ambiguous match |
| Recovery | Capies restart with an existing tagged/identified app; app closes then reopens |
| Persistence | Legacy fixture migration, atomic-write failure, corrupt input, newer schema, preserved profiles |
| Lifecycle | No tracking for unused apps; cancellation/disposal; reveal only windows hidden by Capies |
| Boundaries | Core has no desktop/platform references; Windows does not reference Desktop |

Core tests use fake adapters/window services and controlled completions. They should
not launch real apps, depend on live browser history, or mutate the user's settings.
Windows integration checks use temporary fixtures/profiles when possible and run
only on a supported Windows desktop. Label interactive checks separately.

## Manual/integration acceptance

- Pet click, selected provider, tray double-click, all nine existing menu commands.
- Pose and size submenus; auto-sleep checked state and Automatic-only enabling.
- Arrow keys, Enter, Escape, disabled-item skipping, focus return, dark/high-contrast
  rendering, relevant DPI/monitor configurations.
- Both providers: cold launch, cached show/hide, minimized restore, switch away/back,
  close/reopen, and persisted conversation/profile reuse.
- Generic desktop adapter: third program with no coordinator/UI provider branch.
- Missing executable, missing Edge, timeout, and unsupported/ambiguous windows.
- Shutdown/removal behavior, once its new policy is implemented.

Preserve current behavior in extraction tests; explicitly update acceptance when
a reviewed behavior change is introduced. See baseline differences in
[current-state](current-state.md).

## Performance evidence

Measure separately: menu opening, UI command dispatch, cached show/hide, discovery,
cold launch, session capture, and settings persistence.

Record revision, Windows/build configuration, DPI/monitor context, cold/warm status,
sample count, measurement boundary, median and tail/max. Distinguish request-issued
latency from visibly presented/foreground latency.

Use the baseline on the same machine for regression comparisons. The historical
16.08 ms menu median is not a switching target or an architectural guarantee.
A sub-100 ms warm-command response is an initial evaluation goal, not a verified
service-level objective. Establish actual thresholds during milestone 1.

## Publishing when requested or already authorized

The last menu deployment used this packaging shape:

```powershell
dotnet publish .\GeminiCapyPet.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o .\publish\staging
```

This is a command reference, not evidence of a new deployment. Update the project
path when the solution layout changes; retain packaging compatibility unless a
packaging change is intentional.

Build and validate before replacing the running executable. Stage output, retain
the previous executable, close the verified Capies process gracefully, replace
files, restart, and verify the executable/path and startup result. An API call
returning successfully is not enough to claim the new process remained running.
Preserve profiles and settings. Do not terminate unrelated browser/app processes.

Commit/push and deployment are separate outcomes. Confirm push success explicitly.
Documentation-only work does not require a restart.

## Change report format

- Revision and scope: what changed, what remains proposed.
- Checks: exact command or interactive scenario and observed result.
- Performance: boundary and sample details when measured.
- Unverified: scenarios/platforms not exercised and unresolved limitations.
- Delivery: local changes / commit / push / publish / restart, each with evidence.
