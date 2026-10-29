# Incremental migration

Status at documentation baseline 8d98105: **all runtime milestones below are pending**.
The documentation foundation is created by this docs-only change.
Do not mark a runtime milestone complete merely because its design is documented.

## 1. Establish reproducible behavior

Capture current command, placement, persistence, keyboard, and latency behavior.
Introduce meaningful characterization checks and record known differences rather
than accidentally treating target behavior as current behavior.

Exit evidence: committed runnable checks or clearly documented manual scenarios,
baseline revision/results, and explicit unverified cases.

## 2. Extract coordination without rewriting providers

Introduce coordinator/session contracts and wrap existing Gemini/ChatGPT controllers.
Route pet/tray commands through the coordinator; move provider branching and operation
ownership out of PetWindow. Keep adapter wrappers thin and document temporary legacy
work inside them. Stage request-ordering improvements with their own tests.

Exit evidence: existing app/menu/preferences behavior retained, orchestration tests
pass, no new provider-specific switching logic in UI. Profile paths are unchanged.
Project separation can follow this extraction with buildable intermediate commits;
there is no requirement to rename the executable or namespace first.

## 3. Share Windows services and improve lifecycle

Consolidate identity validation, discovery, cache, positioning, and window operations.
Move capture and slow work off the UI thread, make services lazy, and introduce
explicit cancellation/stale-result handling.

Treat failure-preserving handoff, minimized-window behavior, shared placement, and
exit/removal restoration as explicit behavior changes. Test them individually;
they differ from baseline behavior.

Exit evidence: regression and Windows checks for stale handles, duplicate acquisition,
rapid switching, failed launch, restart recovery, placement, and shutdown.
Performance evidence distinguishes warm operations from cold discovery/launch.

## 4. Register additional software

Add versioned definitions, registry, migration, and the ordinary desktop adapter.
Exercise capability/identity policy with a third real desktop program. Keep existing
Gemini and ChatGPT browser profiles and saved conversations intact.

Exit evidence: a supported third program works through a definition without edits
to coordinator or pet command logic; legacy migration and failure tests pass.

## 5. Build app management UI

Add/edit/remove/reorder software and configure supported placement options.
Generate menu entries from the registry; bind views/view models to shared commands.

Exit evidence: add a supported program through UI with no code change; state persists
after restart; keyboard access and existing pet controls remain usable.

## Deferred extensions

A separate top-right icon panel, global hotkeys, packaged/URI app adapters, more pet
animations, and reminders can follow demonstrated needs. They are not implemented
or promised by the architecture foundation. No third-party plugin loader is planned.

## Updating progress

For each completed gate, add its implementation commit, exact checks/results,
remaining limitations, and any linked ADR. Keep incomplete gates marked pending.
Record partial implementation as partial; do not infer completion from file names.
