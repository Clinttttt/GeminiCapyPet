using System.Diagnostics;
using System.Windows;

namespace GeminiCapyPet;

/// <summary>Open ChatGPT in the user's default browser, reusing their existing signed-in session.</summary>
internal static class ChatGptLauncher
{
    public static void Open()
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://chatgpt.com/")
            {
                UseShellExecute = true
            });
            PetLog.Info("Opened ChatGPT in default browser.");
        }
        catch (Exception ex)
        {
            PetLog.Info("ChatGPT launch failed: " + ex);
            System.Windows.MessageBox.Show(ex.Message, "Capies - ChatGPT",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}

