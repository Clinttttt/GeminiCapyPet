namespace GeminiCapyPet;
internal static class SoftwareComposition
{
    internal static AppCoordinator Create(GeminiWindowController gemini, ChatGptLauncher chatGpt) =>
        new(entry => entry.Id switch
        {
            "gemini" => new DelegateManagedApp(() => gemini.IsVisibleCached, gemini.EnsureVisibleAsync, gemini.HideIfCached),
            "chatgpt" => new DelegateManagedApp(() => chatGpt.IsVisibleCached, chatGpt.EnsureVisibleAsync, chatGpt.HideIfCached),
            _ => new DesktopManagedApp(entry)
        });
}
