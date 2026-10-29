# Working on Capies

These instructions apply to this repository. Explicit user instructions and the
host's higher-priority instructions take precedence. Respect authorization already
provided in the session; this file adds no approval requirement.

## Read before changing behavior

1. [Documentation map](docs/README.md).
2. [Verified baseline](docs/current-state.md), then inspect the current source.
3. [Target architecture](docs/architecture.md) and
   [engineering patterns](docs/engineering-patterns.md).
4. Use the repository [Capies development skill](.agents/skills/capies-development/SKILL.md)
   for implementation, refactoring, adding apps, or architecture reviews.

## Evidence and status

- Source and fresh tool results establish what exists. Design documents establish
  the intended direction. Conversation summaries are context, not verification.
- Every new capability must be labeled implemented, proposed, or unknown.
  Never treat a proposed type, folder, test suite, or command as already present.
- Read referenced files and relevant call sites before editing. Do not invent APIs,
  dependencies, window capabilities, measurements, or successful checks.
- If code and documentation differ, identify the discrepancy and update the
  relevant document in the same change. Preserve dated baseline evidence.
- A build proves compilation, not window behavior. Publish, restart, commit, and
  push are separate outcomes; report each only with supporting tool results.
- Record unrun checks and limitations directly. Do not claim documentation can
  guarantee correctness or eliminate hallucination.

## Design rules for new work

- Preserve existing behavior during extraction. The known gaps in
  [current-state](docs/current-state.md) are not silently fixed by a refactor.
- Move toward a single coordinator for app commands, adapters for app-specific
  launch/identification, and shared Windows services. Avoid new provider branches
  in PetWindow or cross-provider calls inside adapters.
- Use composition and constructor injection at genuine boundaries. Do not add
  interfaces for every class, service locators, speculative plugin loaders, or
  extra running services.
- Keep window discovery, WMI, database access, and file I/O off UI rendering and
  property getters in the target design. Existing exceptions must be tracked.
- Preserve browser profiles, conversation files, pet preferences, menu commands,
  keyboard access, and the checked/disabled auto-sleep behavior.
- Validate cached window identity before acting. Only manage verified app windows.
- Runtime state and persisted configuration are separate; never persist HWNDs/PIDs.
- UI changes use the shared command path. Visuals do not own app-switching policy.

## Working and verification

- Check git status and applicable nested instructions first. Preserve unrelated edits.
- Prefer rg for search; use PowerShell Select-String/Get-ChildItem when unavailable.
- Follow [validation](docs/validation.md). Run checks appropriate to the changed
  behavior, not unrelated suites or repeated builds without a reason.
- Add meaningful tests for orchestration, identity, migration, and cancellation.
  Do not add tests that merely repeat trivial implementation details.
- Keep scratch work outside SDK compile globs, under a work directory. The current
  root SDK project recursively includes C# sources; do not place temporary .cs
  files or generated test projects beneath it without explicit exclusions.
- Update [roadmap status](docs/roadmap.md) only with implementation and validation
  evidence. Document architectural departures in an ADR.
- Documentation-only changes require link/source-reference and diff checks.
  They do not require publishing or restarting Capies.
