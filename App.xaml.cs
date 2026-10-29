using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace GeminiCapyPet;

public partial class App : System.Windows.Application
{
    private Mutex? _instanceMutex;
    private bool _ownsMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        _instanceMutex = new Mutex(true, @"Local\GeminiCapyPet.SingleInstance", out _ownsMutex);
        if (!_ownsMutex)
        {
            // Re-launching the shortcut should reveal the existing pet, not create duplicates.
            var existingWindow = FindWindow(null, "Gemini Capy Pet");
            if (existingWindow != IntPtr.Zero)
            {
                ShowWindow(existingWindow, 9);
                SetForegroundWindow(existingWindow);
            }
            Shutdown();
            return;
        }
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_instanceMutex != null)
        {
            if (_ownsMutex) _instanceMutex.ReleaseMutex();
            _instanceMutex.Dispose();
            _instanceMutex = null;
        }
        base.OnExit(e);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string? className, string? windowName);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr window, int command);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr window);
}
