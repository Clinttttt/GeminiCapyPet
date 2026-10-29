# Software library: first implementation

Status: implemented incremental feature; full architecture migration remains partial.

## Use

Open Capies' menu, then **Manage software...**. Add an existing .exe, choose an
entry, and press **Use this app**. Added software appears in the Software submenu.
The pet and its icon button toggle the selected app. The menu button opens the
existing dark menu. The minus button hides Capies to the tray; tray double-click
or **Show Capies** restores it. Remove affects the registration only.

Gemini and ChatGPT remain built-in. Added apps use their executable icon; the two
built-ins have distinct sparkle/chat symbols. Selection persists in software.json
under the existing local application data directory, with an atomic replacement
and .bak backup. Initial selection migrates from ActiveChatProvider. Pet settings,
browser profile paths, and conversation files are retained.

## Structure and intentional interim decisions

- [SoftwareCatalog](../Features/Software/SoftwareCatalog.cs): versioned registry,
  selection, add/remove and storage. Unknown/corrupt versions are read-only.
- [AppCoordinator](../Features/Software/AppCoordinator.cs): serialized app commands;
  shows destination before hiding the previous app.
- [SoftwareComposition](../Infrastructure/Software/SoftwareComposition.cs): wiring
  of existing browser controllers and the generic desktop adapter.
- [DesktopManagedApp](../Infrastructure/Software/DesktopManagedApp.cs): exact
  executable matching, process/handle cookie validation, reuse, top-right placement.
- [SoftwareWindow](../Features/Software/SoftwareWindow.xaml): management UI.
- [SoftwareTests](../tests/Capies.Characterization.Tests/SoftwareTests.cs): six
  additional tests alongside the preserved 18 characterization tests.

This slice uses logical modules in the existing project. Browser discovery/tracking
remains in legacy controllers behind thin adapters. Queueing is serialized FIFO,
not the future generation/cancellation model: a cold launch can delay later requests.
The manager uses a small presentation model with UI handlers rather than the full
proposed MVVM layer. Catalog storage is still cohesive within the catalog and can be
extracted to a store port as migration proceeds. These choices preserve a buildable
increment and do not mark the entire architecture roadmap complete.

## Limits and evidence

- Standard single-main-window executables only. Multiple matching windows produce
  an explicit message rather than choosing an unrelated window.
- Launcher executables, packaged apps, elevation boundaries, and custom arguments
  are not supported generically yet.
- Added apps are positioned on initial attachment; warm restores retain placement.
  Fixed-size windows retain their dimensions. OS foreground permission is not guaranteed.
- Removal/exit reveals a surviving desktop window hidden by Capies, without
  relaunching closed programs. Browser shutdown keeps its legacy behavior.
- Automated suite: 24 passed in this implementation session (18 existing + 6 new).
- Isolated Windows fixture passed attach/show, hide, restore, right placement,
  closed-handle invalidation, and manager rendering.
- Browser live switching, cold-launch timing, multi-monitor/DPI matrix, high-contrast
  manager styling and full interactive keyboard acceptance remain unverified.
- Original Gate 1 evidence is retained as historical/partial, not overwritten.

Live deployment verification: the published executable matched the staged SHA-256, the new process remained running, the UI Automation collapse button hid the pet, and single-instance relaunch restored it. Tray double-click restoration follows the existing handler but was not separately automated.
