using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace GeminiCapyPet;

internal sealed class GeminiWindowController
{
    private const string Url = "https://gemini.google.com/app";
    private static readonly string SessionFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GeminiCapyPet", "last-conversation.txt");
    private const int SwHide = 0;
    private const int SwShow = 5;
    private const int SwRestore = 9;
    private const string WindowTag = "GeminiCapyPet.MainWindow";
    private readonly string _edgePath;
    private readonly string _profileDirectory;
    private IntPtr _window;
    private readonly GeminiConversationTracker _tracker;
    private const int DefaultWidth = 600;
    private const int DefaultHeight = 800;
    private const int EdgeMargin = 12;

    public GeminiWindowController()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "Microsoft", "Edge", "Application", "msedge.exe")
        };
        _edgePath = candidates.FirstOrDefault(File.Exists)
            ?? throw new FileNotFoundException("Microsoft Edge is required for Gemini.");
        _profileDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GeminiCapyPet", "EdgeProfile");
        _tracker = new GeminiConversationTracker(_profileDirectory);
        _tracker.Start();
    }

    public bool IsVisible => FindWindow() is var h && h != IntPtr.Zero && IsWindowVisible(h);
    public void StopTracking() => _tracker.Stop();

    public async Task<bool> ToggleAsync()
    {
        var hwnd = FindWindow();
        if (hwnd != IntPtr.Zero)
        {
            if (IsWindowVisible(hwnd) && !IsIconic(hwnd))
            {
                ShowWindow(hwnd, SwHide);
                return false;
            }
            BringGeminiForward(hwnd);
            return true;
        }

        _tracker.CaptureLatest();
        // Always reuse the same Edge user-data-dir. Never delete or recreate it:
        // this preserves Google's authentication cookies and Gemini history.
        Directory.CreateDirectory(_profileDirectory);
        var startUrl = GetSavedConversationUrl();
        PetLog.Info("Opening Gemini with persistent Edge profile");
        using var process = Process.Start(new ProcessStartInfo(_edgePath)
        {
            Arguments = $"--user-data-dir=\"{_profileDirectory}\" --app=\"{startUrl}\" --new-window --no-first-run",
            WorkingDirectory = Path.GetDirectoryName(_edgePath)!,
            UseShellExecute = false
        }) ?? throw new InvalidOperationException("Unable to launch Microsoft Edge.");

        // Edge can reuse an existing browser process. Its app window can belong
        // to a different PID than the process returned by Process.Start.
        for (var attempt = 0; attempt < 100; attempt++)
        {
            await Task.Delay(150);
            hwnd = FindWindow();
            if (hwnd == IntPtr.Zero) continue;
            BringGeminiForward(hwnd);
            return true;
        }
        throw new TimeoutException("Gemini did not open within 15 seconds. Check Microsoft Edge.");
    }

    private static string GetSavedConversationUrl()
    {
        try
        {
            if (File.Exists(SessionFile))
            {
                var saved = File.ReadAllText(SessionFile).Trim();
                if (Uri.TryCreate(saved, UriKind.Absolute, out var uri)
                    && uri.Scheme == Uri.UriSchemeHttps
                    && uri.Host.Equals("gemini.google.com", StringComparison.OrdinalIgnoreCase)
                    && uri.AbsolutePath.StartsWith("/app/", StringComparison.OrdinalIgnoreCase))
                    return uri.GetLeftPart(UriPartial.Path);
            }
        }
        catch (Exception ex) { PetLog.Info("Saved conversation unavailable: " + ex.Message); }
        return Url;
    }

    private IntPtr FindWindow()
    {
        if (_window != IntPtr.Zero && IsWindow(_window))
            return _window;

        _window = IntPtr.Zero;
        var candidatePids = Process.GetProcessesByName("msedge")
            .Where(p =>
            {
                try
                {
                    using var searcher = new System.Management.ManagementObjectSearcher(
                        $"SELECT CommandLine FROM Win32_Process WHERE ProcessId={p.Id}");
                    foreach (System.Management.ManagementObject item in searcher.Get())
                    {
                        var command = item["CommandLine"]?.ToString() ?? "";
                        if (command.Contains(_profileDirectory, StringComparison.OrdinalIgnoreCase)
                            && !command.Contains("--type=", StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }
                catch { }
                return false;
            })
            .Select(p => (uint)p.Id).ToHashSet();

        // Prefer the previously tagged Gemini window (also works when hidden).
        EnumWindows((h, _) =>
        {
            if (GetProp(h, WindowTag) == IntPtr.Zero) return true;
            GetWindowThreadProcessId(h, out var pid);
            if (!candidatePids.Contains(pid)) return true;
            _window = h;
            return false;
        }, IntPtr.Zero);
        if (_window != IntPtr.Zero) return _window;

        // Find Gemini by title AND by the dedicated Edge profile's process ID.
        // Do not accidentally hide an unrelated browser window.
        EnumWindows((h, _) =>
        {
            GetWindowThreadProcessId(h, out var pid);
            if (!candidatePids.Contains(pid)) return true;
            var length = GetWindowTextLength(h);
            if (length == 0) return true;
            var title = new StringBuilder(length + 1);
            GetWindowText(h, title, title.Capacity);
            if (!title.ToString().Contains("Gemini", StringComparison.OrdinalIgnoreCase))
                return true;
            _window = h;
            SetProp(h, WindowTag, new IntPtr(1));
            return false;
        }, IntPtr.Zero);
        return _window;
    }

    private void BringGeminiForward(IntPtr hwnd)
    {
        if (IsIconic(hwnd)) ShowWindow(hwnd, SwRestore);
        else if (!IsWindowVisible(hwnd)) ShowWindow(hwnd, SwShow);
        PositionOnRight(hwnd);
        // A short topmost pulse reliably raises the window even when Edge
        // is behind another application; do not leave it always-on-top.
        SetWindowPos(hwnd, new IntPtr(-1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0040);
        SetWindowPos(hwnd, new IntPtr(-2), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0040);
        SetForegroundWindow(hwnd);
        PetLog.Info("Gemini raised and positioned at top-right.");
    }

    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();

    private void PositionOnRight(IntPtr hwnd)
    {
        // The example uses a compact ~600px-wide chat aligned with the right
        // edge of the primary monitor, leaving a small gap above the taskbar.
        if (!GetWindowRect(hwnd, out var current)) return;
        var monitor = MonitorFromWindow(hwnd, 2);
        var info = new MonitorInfo { cbSize = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref info)) return;
        var work = info.rcWork;
        var availableWidth = work.Right - work.Left;
        var availableHeight = work.Bottom - work.Top;
        var width = Math.Min(DefaultWidth, Math.Max(300, availableWidth - EdgeMargin * 2));
        var height = Math.Min(DefaultHeight, Math.Max(400, availableHeight - EdgeMargin * 2));
        var x = work.Right - width;
        var y = work.Top;
        if (!SetWindowPos(hwnd, IntPtr.Zero, x, y, width, height, 0x0004 | 0x0010))
            PetLog.Info("Unable to position Gemini on the right.");
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int cbSize;
        public Rect rcMonitor;
        public Rect rcWork;
        public uint dwFlags;
    }
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out Rect rect);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool SetProp(IntPtr hwnd, string name, IntPtr value);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetProp(IntPtr hwnd, string name);
    private delegate bool EnumWindowsCallback(IntPtr hwnd, IntPtr param);
    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr param);
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hwnd, StringBuilder buffer, int max);
    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hwnd);
    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hwnd);
    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hwnd, int command);
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hwnd);
}
