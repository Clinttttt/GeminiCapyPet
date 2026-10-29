# Verified implementation baseline

Status: **implemented source baseline**, inspected 2026-10-29 (Asia/Singapore).
Runtime revision: `8d98105`. Reinspect HEAD before using this snapshot for a change.

## Source map

| Concern | Source and symbols | Observed implementation |
| --- | --- | --- |
| Packaging | [GeminiCapyPet.csproj](../GeminiCapyPet.csproj) | One net10.0-windows WinExe; WPF and WinForms enabled |
| Startup | [App.xaml.cs](../App.xaml.cs), OnStartup / OnExit | Named single-instance mutex; second launch attempts to reveal existing pet |
| UI and orchestration | [PetWindow.xaml.cs](../Features/Pet/PetWindow.xaml.cs), SetupTray / ToggleGeminiAsync / ToggleChatGptAsync | Pet gestures, provider selection, menu construction, busy/pending flags, preferences, idle behavior |
| Gemini | [GeminiWindowController.cs](../Features/Gemini/GeminiWindowController.cs), ToggleAsync / FindWindow / HideIfCached | Edge profile, cached/tagged window discovery, launch, restore, initial positioning |
| ChatGPT | [ChatGptLauncher.cs](../Features/ChatGPT/ChatGptLauncher.cs), ToggleAsync / FindChatWindow / ShowAndPosition | Separate Edge profile, cached/tagged discovery, launch and positioning |
| Gemini restoration | [GeminiConversationTracker.cs](../Features/Gemini/GeminiConversationTracker.cs), Start / CaptureLatest | Read-only SQLite history query on a five-second DispatcherTimer |
| ChatGPT restoration | [ChatGptConversationTracker.cs](../Features/ChatGPT/ChatGptConversationTracker.cs), Start / CaptureLatest / GetRestoreUrl | Read-only SQLite history query and URL normalization |
| Pet state | [PetPreferences.cs](../Features/Pet/PetPreferences.cs), Load / Save | JSON preferences with defaults and validation; direct file writes, no schema version |
| Customization | [PetSettingsWindow.xaml.cs](../Features/Customization/PetSettingsWindow.xaml.cs) | Calls PetWindow directly |
| Animation | [SpriteAnimator.cs](../Features/Pet/SpriteAnimator.cs) | Pet pose and animation behavior |
| Menu visuals | [GlassContextMenuStrip.cs](../Infrastructure/Tray/GlassContextMenuStrip.cs), InitializeTheme / GlassMenuRenderer | Native menu, charcoal gradient, 96% opacity, matched submenus, high-contrast fallback |
| Logging | [PetLog.cs](../Infrastructure/Diagnostics/PetLog.cs), Info | Synchronous best-effort append to local pet.log |

## Compatibility baseline

- Pet click toggles the selected provider; provider menu actions select Gemini or
  ChatGPT and persist that selection.
- The root menu retains nine commands: Gemini, ChatGPT, customize, pose, size,
  auto-sleep, play sleep animation, reset position, and exit. Pose has five choices;
  size has four presets plus custom size and a separator.
- Auto-sleep is checked from preferences, but enabled only for Automatic pose.
  Idle sleep requires both providers hidden and 45 seconds of inactivity.
- A minimized window should be distinguished from a hidden window in the target;
  current behavior differs by entry point. PetWindow first checks IsVisible,
  which can include minimized windows, while controller ToggleAsync checks IsIconic.
- Gemini initially positions a compact window at the top-right of its monitor's
  work area, then its cached restore avoids repositioning.
- ChatGPT calls ShowAndPosition for both initial display and cached restore.
- Width/height defaults are about 600 by 800 physical pixels, with different
  clamping logic in the two implementations. Neither implements the proposed
  shared, configurable, DPI-aware placement policy.
- Menu behavior remains WinForms; glass means uniform translucency, not acrylic blur.

## Persistence that must survive migration

Root: `%LOCALAPPDATA%\GeminiCapyPet`.

| Path under that root | Existing purpose |
| --- | --- |
| settings.json | Pose, Size, AutoSleep, ActiveChatProvider, Left, Top |
| EdgeProfile/ | Gemini browser sign-in and profile |
| ChatGptEdgeProfile/ | ChatGPT browser sign-in and profile |
| last-conversation.txt | Saved Gemini conversation URL |
| last-chatgpt-conversation.txt | Saved ChatGPT conversation URL |
| pet.log | Local diagnostics |

Do not rename, delete, merge, or regenerate these profiles as part of restructuring.
The future settings migration must preserve them explicitly.

## Known gaps; not fixed by these documents

1. Provider controllers are constructed by PetWindow and start tracking eagerly.
   Missing Edge can fail construction even before a provider is used.
2. IsVisible performs discovery on cache miss. OnIdleTick calls visibility checks
   on the UI thread. Gemini discovery still includes WMI queries per Edge process;
   ChatGPT batches its process query.
3. Trackers perform SQLite/file work on DispatcherTimer callbacks. Their work
   can block the UI; synchronous log and preference writes also remain.
4. Cached handle validation uses IsWindow; ownership/lifetime verification needs
   strengthening. Title matching is not a universal app identity strategy.
5. The current busy/pending mechanism is provider-specific, not a general request
   queue with cancellation and stale-completion protection.
6. Switching hides the previous provider before the destination has successfully
   launched. The proposed failure-preserving handoff is a behavior change.
7. Shutdown stops trackers and saves preferences. It does not implement the
   proposed policy to reveal surviving windows hidden by Capies.
8. Settings have no versioned migration or atomic replacement protocol.
9. There is no app registry, generic desktop adapter, Add Software UI, Core/Windows/
   Desktop project split, or committed automated test project at this baseline.

## Evidence limits

- The prior menu implementation session recorded a Release build with zero
  warnings/errors, successful publish/restart, and push of 8d98105.
- An ad hoc Windows harness outside this repository exercised menu command
  inventory, check synchronization, arrow keys, submenu Escape, disabled-item
  navigation, and repeated opening at 120 DPI.
- Its final recorded warm run was 25 samples: 16.08 ms median, 32.57 ms maximum.
  This is menu timing only, not provider switching, cold launch, or a service-level
  guarantee. The harness is not a reproducible repository test suite.
- High contrast, multiple monitors, other DPI scales, and universal software
  compatibility were not established by that harness.
- This documentation pass verifies source references; it does not rerun those
  historical runtime checks.
