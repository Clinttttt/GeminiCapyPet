# Engineering patterns

Status: **rules for new work toward the target**. Existing exceptions are listed in
[current-state](current-state.md). Follow the phased [roadmap](roadmap.md); do not
mix unrelated behavior changes into a compatibility extraction.

## Composition and OOP

Use small cohesive classes and constructor injection at behavior/platform boundaries.
Create dependencies in Desktop composition, not through globals or service locators.
Prefer shared services plus adapter composition over a deep BaseAppController tree.

An interface earns its place when it separates a platform dependency, represents
multiple real strategies, or allows meaningful policy tests. Plain data records,
simple internal helpers, and visual-only classes need not have interfaces.

Use explicit typed results for acquisition and operations: success with a verified
reference, not found, unsupported, canceled, timeout, or failed. Preserve underlying
diagnostic information without exposing implementation details in normal UI flows.
Do not hide a failure behind a boolean that cannot distinguish its cause.

## Adding an app

1. Confirm whether an existing adapter supports its launch and window identity.
2. Add a definition with a stable ID and typed launch/placement configuration.
3. Use a specialized adapter only for behavior the common adapter cannot express.
4. Add optional restoration behavior separately from window manipulation.
5. Register the definition through the registry; generate UI entries from it.
6. Verify launch, reuse, hide, minimize/restore, switching, close/reopen, failure,
   and restart recovery for that app.

For a supported ordinary executable, adding it should eventually require data only.
Do not add another provider enum branch, hardcoded menu action, or pairwise
"hide every other provider" sequence in PetWindow.

Use argument lists/typed launch specifications; do not construct arbitrary shell
scripts from app display names or settings. Preserve executable paths containing
spaces. Deleting a registration does not delete the executable or its user data.

## Commands and concurrency

The coordinator owns switching policy. Adapters never call another adapter.
Use explicit Show/Hide/Acquire operations beneath the coordinator, so retries
cannot accidentally invert visibility.

Keep desired state separate from observed state. Associate asynchronous work with
an operation generation; reject stale completions before focus, placement, and
handoff. Deduplicate acquisition for the same app. Cancellation must be cooperative,
bounded, and distinguish waiting from the external process lifetime.

Dispose cancellation sources and subscriptions when operations end. Do not hold a
global lock across a slow launch or publish UI events while holding an internal lock.
Fake adapters and a controllable clock should make race scenarios deterministic.

## Fast window access

| Allowed fast path | Explicit background work |
| --- | --- |
| Read an in-memory snapshot | Enumerate candidate windows |
| Validate cached identity with bounded native checks | Resolve ambiguous process/profile identity |
| Request show/hide for a verified handle | Query WMI if needed as a fallback |
| Return cached menu/idle state | Read browser history or migrate settings |

Do not put discovery, SQL, directory creation, synchronous logging, or settings
writes in rendering, a property getter, or an idle timer's UI callback.
Batch/share discovery across interested apps. Reconcile on launch, invalidation,
or explicit recovery; event hooks may improve this but must not be assumed reliable
without fallback. Zero work should be scheduled merely because an unused app is registered.

Check ownership and lifetime before acting on a cached HWND. Discard stale entries.
Use titles only as supplemental evidence. Handle access-denied or unsupported
operations as app capability/outcome information, not a reason to manipulate a
loosely matched window.

## UI and feature boundaries

Use MVVM for app management and customization. View code may handle drag coordinates,
native tray integration, or drawing. It must delegate app policy through commands.
The current WinForms menu and renderer remain usable during the WPF refactor.

Map operation state into UI feedback without blocking the dispatcher. Keep native
keyboard navigation, check states, disabled-item behavior, high-contrast support,
and DPI readability. A renderer does not start apps or write preferences.

New features have their own state/service boundary and consume focused contracts.
Use direct typed notifications initially; add an event bus only when a demonstrated
need justifies its lifecycle and debugging cost. Pet idle rules consume a cached
app-state snapshot instead of asking every integration to discover its windows.

## Persistence and diagnostics

Validate settings on load and write a schema version. Migrations must preserve
existing provider/profile locations and unknown data where applicable.
Use atomic replacement with a backup. For a newer unsupported schema, avoid
overwriting it with defaults; surface the incompatibility and preserve the file.
Coordinate writes and ensure pending valid changes flush on orderly shutdown.

Diagnostics identify app ID, operation ID/generation, phase, duration, and outcome.
Avoid logging full browser URLs or process command lines unnecessarily. Log state
transitions and failures without synchronous hot-path disk writes or unbounded
repeated messages.

## Review examples

| Problematic change | Preferred change |
| --- | --- |
| New ToggleNotepadAsync in PetWindow | Registered definition using DesktopAppAdapter |
| IsVisible getter executes WMI | Cached snapshot plus explicit reconciliation |
| Adapter hides Gemini before launching its app | Coordinator handles handoff |
| Menu rebuild reads settings from disk | Menu consumes current registry/view model |
| Every new app starts a timer in its constructor | Lifecycle starts needed work lazily |
| Base class with dozens of provider switches | Shared services plus focused adapters |
| Documentation says "tests pass" without results | Exact checks, revision, outcomes, and unrun items |

See [validation](validation.md) for evidence and completion requirements.
