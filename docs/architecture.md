# Managed-app architecture

Status: **target design; implementation pending**.
See [baseline](current-state.md), [decision](decisions/0001-managed-app-architecture.md),
and [migration gates](roadmap.md).

## Purpose and scope

Capies remains one Windows desktop companion process. Users can eventually register
software and activate it through the pet, tray, or another UI. Supported software
shares launch, show/hide, focus, and placement behavior.

Top-right placement refers to the actual software window. A separate icon panel is
an optional future UI, not an implemented or required part of this design.
The first generic integration targets ordinary desktop executables. Packaged apps,
custom URI launchers, elevated apps, and unusual multi-window apps require evaluated
support rather than an assumption of universal compatibility.

## Dependency boundaries

Proposed projects:

```text
src/
  Capies.Core/
    Apps/         Definitions, sessions, coordinator, ports
    Pet/          Pet behavior and idle-state rules
    Settings/     Models and migration contracts
  Capies.Windows/
    Apps/         Browser and desktop adapters
    Windowing/    Identity, cache, discovery, placement, Win32
    Tracking/     Conversation capture and restoration
    Persistence/  JSON storage and migrations
    Diagnostics/
  Capies.Desktop/
    Pet/          WPF view and view model
    Tray/         Menu construction and native renderer
    Customization/
    AppLibrary/   Add/edit/remove/reorder UI
    Composition/  Startup, wiring, shutdown
tests/
  Capies.Core.Tests/
  Capies.Windows.Tests/
```

These paths and types do not exist at the baseline. Initial extraction may introduce
logical modules within the existing project before moving files.

- Core has no WPF, WinForms, System.Management, SQLite, or Win32 dependencies.
- Windows references Core and implements its platform contracts.
- Desktop references Core; its composition root references Windows to wire concrete
  implementations. Views and view models do not call Windows internals directly.
- Core never references Desktop or Windows. Windows never references Desktop.
- A testable UI dispatcher abstraction or explicit event marshalling bridges
  background state updates to WPF/WinForms.

## Responsibilities

| Proposed concept | Owns | Must not own |
| --- | --- | --- |
| AppDefinition | Stable ID, display metadata, typed launch specification, placement preference | HWND, PID, credentials, mutable session state |
| AppRegistry | Configured definitions, ordering, registration changes | Launching or UI creation |
| AppCoordinator | Commands, request ordering, desired state, switching, handoff recovery | Win32 calls or provider-specific URLs |
| AppSession | Observed state, verified window reference, generation, in-flight operation | Persisted handles or WPF objects |
| IAppAdapter | Acquire/identify a window for an app kind; expose supported capabilities | Hiding other apps, toggling policy, pet/menu updates |
| IWindowService | Validate, inspect, show, hide, focus, position verified windows | Choosing the selected app |
| ISessionRestorer | Optional app-specific restore data and capture | Mandatory background polling for every app |
| SettingsStore | Versioned read, validation, migration, atomic write | Window ownership or UI state transitions |

Contract names are design concepts, not final C# signatures. Before implementation,
define cancellation behavior, ownership, results, and threading for each method.

An app adapter acquires a verified window reference. The coordinator then requests
explicit window operations. There is no second independent Toggle implementation
inside each adapter.

## Command and state model

User commands: Activate(appId), Toggle(appId), Hide(appId).
Persisted SelectedAppId identifies the user's preferred target. Runtime VisibleAppId
describes what is actually visible. Neither alone proves a window is alive.

Observed states: Unknown, NotRunning, Launching, Visible, Hidden, Minimized,
Unavailable. Track desired visibility separately; a request is not an observation.

| Request/condition | Target behavior |
| --- | --- |
| Toggle a verified visible app | Hide that managed window |
| Toggle hidden or minimized app | Show/restore it |
| Activate any app | Make it visible; never toggle it off |
| Unknown or absent window | Reconcile in background, then launch only if necessary |
| Destination cannot be acquired/shown | Keep or restore the previous managed app; expose error |
| Newer selection supersedes a launch | Cancel waiting where possible; ignore stale focus/placement commits |

The coordinator owns serialized state transitions. It must not hold the command
queue blocked while a slow process launch is awaited. Acquire work runs outside the
state-transition loop and returns an operation generation.

Repeated toggles update desired visibility deterministically: two toggles while
initially hidden return to a hidden desire. Requests for a different app supersede
obsolete activation intent. One acquisition per app can run at a time.

Only a result matching the current generation may commit a foreground handoff.
Cancellation of a wait does not imply the external process stopped. Track a late
window; do not focus it or kill the external process. Hide a late Capies-owned
window only when ownership and the desired state justify it.

## Handoff, identity, placement

Acquire and validate destination B while leaving A usable. Show/restore B, inspect
the result, and hide A only as part of a successful current handoff. If a later step
fails, restore A where possible and report observed state. OS focus requests can be
denied; do not claim a requested foreground action was guaranteed.

Only verified managed windows participate. Distinguish windows launched by Capies
from pre-existing windows deliberately adopted by the user. Multi-window apps need
an explicit primary-window strategy or a picker when matching is ambiguous.

A cached reference must validate handle, owner process/lifetime, and integration
identity before reuse. PID or title alone is insufficient. Process-launch PID may
differ from the eventual app-window PID, especially for browsers.

Placement is a shared policy with top-right as the proposed default. Resolve a
monitor and work area; convert configured logical units to pixels; clamp to usable
bounds. Honor resize capability and app minimum size. Reposition on initial attach
or explicit placement changes; avoid repeated resize work on warm restores.
This unification changes ChatGPT's current reposition-on-restore behavior and must
be introduced and verified explicitly.

## Persistence and lifecycle

Store app definitions and user preferences separately from runtime sessions. Use
stable IDs, a schema version, migration fixtures, atomic replacement, and a last-good
backup. Map existing provider names to app IDs while preserving all paths listed in
[current-state](current-state.md). Never persist HWNDs or PIDs.

Create adapters and trackers lazily. Run capture off the UI thread, avoid overlapping
queries, and stop services on removal/shutdown. The lifecycle owner disposes all
workers and event subscriptions.

Proposed exit/removal policy: reveal surviving windows that Capies itself hid,
without launching a closed app or terminating software. Do not alter unrelated
windows, delete app data, or recreate browser profiles. Introduce this as a tested
behavior change, not as an incidental extraction.

## Decisions to settle when their milestone begins

- Default monitor selection: pet monitor, cursor monitor, or saved per-app monitor.
- User choice between exclusive switching and keeping several managed apps visible.
  Start with the existing exclusive-switching intent.
- Adoption/picker behavior for ambiguous pre-existing windows.
- Capability UX for programs that cannot resize, hide, or restore sessions.
- Exact schema and public signatures after migration fixtures and adapter spikes.

Resolve routine details within the user's scope; record decisions in an ADR. Ask for
clarification only where a product choice materially affects the requested result.
