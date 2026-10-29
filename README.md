# Gemini Capy Pet (.NET 10 + WPF)

A transparent, draggable Windows desktop companion that uses the 25-frame
Capies-sleep.png sprite sheet and toggles dedicated Gemini and ChatGPT browser app windows.

## Development documentation

Start with the [documentation map](docs/README.md), [verified current state](docs/current-state.md),
and [target architecture](docs/architecture.md). The managed-app refactor is planned;
this documentation does not mean Add Software or the new project split exists yet.

Coding assistants should read [AGENTS.md](AGENTS.md) and the repository-local
[Capies development skill](.agents/skills/capies-development/SKILL.md).

## Start

Run `dotnet run --project C:\dev\GeminiCapyPet\GeminiCapyPet.csproj`.

Or run the published executable under `publish\GeminiCapyPet.exe`.

## Controls

- Left click Capies: open/hide the selected provider (Gemini or ChatGPT).
- Left drag Capies: reposition on the desktop.
- Hover: wake up and show a tooltip.
- Right-click Capies / system-tray icon: **Customize pet...**, pose, size,
  automatic sleep toggle, reset position, and exit.
- Customize window: choose Automatic/Standing/Sitting/Lying down/Sleeping,
  resize the pet live with the 140–360 px slider, and toggle auto-sleep.
- Position, pose, size and auto-sleep are saved across restarts in
  %LOCALAPPDATA%\\GeminiCapyPet\\settings.json.
- With Automatic pose and auto-sleep enabled, Capies sleeps after 45 seconds of
  inactivity while both Gemini and ChatGPT are hidden.
- On the first Gemini launch, sign in to Google if asked. The dedicated Edge
  profile is persisted under %LOCALAPPDATA%\GeminiCapyPet\EdgeProfile.

## Tray menu

The pet and tray icon share a charcoal glass-style native menu with subtle
translucency, rounded hover highlights, and matching pose/size submenus.
Arrow keys, Enter, Escape, checked choices, and disabled auto-sleep behavior
continue to use WinForms menu handling. Windows High Contrast uses opaque
system colors. Windows 11 corner rounding is optional; the menu also works
without it. The glass effect uses lightweight 96% opacity, not acrylic blur.

Menu styling is in `Infrastructure/Tray/GlassContextMenuStrip.cs`; provider
switching and command handlers are independent of the renderer.

## Implementation

- C# / WPF on .NET 10; transparency and Topmost Window.
- Sprite animation is 25 frames, 5 columns x 5 rows, each 256 px.
- Breathing is animated using WPF DoubleAnimation.
- Uses the *real Gemini website* launched in Microsoft Edge app mode, not an
  unofficial API or embedded browser. Requires Microsoft Edge.
- Show/hide is done using native Win32 window handles from that Edge process.
- Closing the pet doesn't delete your Edge profile or browser sign-in.
- The pet is not affiliated with Google or OpenAI.

## Notes

This first version uses the supplied sleeping sprite sheet for wake/sleep,
and subtle scaling for idle breathing. More sprite sheets can be added later
for walking, jumping and waving.

## Added software

Use **Menu > Manage software...** to add a Windows .exe, select it, or remove its registration. Click the app icon or pet to show/hide the selected app. The minus button collapses Capies into the tray; double-click the tray icon to restore it. See [software library details](docs/software-library.md).
