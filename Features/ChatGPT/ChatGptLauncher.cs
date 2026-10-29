using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace GeminiCapyPet;

/// <summary>Dedicated compact ChatGPT window, independent of Gemini's Edge profile.</summary>
internal sealed class ChatGptLauncher
{
    private const string Url = "https://chatgpt.com/";
    private readonly string _profile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GeminiCapyPet", "ChatGptEdgeProfile");
    private readonly string _edge = new[]
    {
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Microsoft", "Edge", "Application", "msedge.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "Microsoft", "Edge", "Application", "msedge.exe")
    }.FirstOrDefault(File.Exists) ?? throw new FileNotFoundException("Microsoft Edge not installed.");
    private const string WindowTag = "GeminiCapyPet.ChatGptWindow";
    private IntPtr _window;
    private readonly ChatGptConversationTracker _tracker;

    public ChatGptLauncher()
    {
        _tracker = new ChatGptConversationTracker(_profile);
        _tracker.Start();
    }

    public void StopTracking() => _tracker.Stop();
    public bool IsVisible => FindChatWindow() is var hwnd && hwnd != IntPtr.Zero && IsWindowVisible(hwnd);
    public void Hide()
    {
        var hwnd = FindChatWindow();
        if (hwnd != IntPtr.Zero && IsWindowVisible(hwnd)) ShowWindow(hwnd, 0);
    }

    public async Task ToggleAsync()
    {
        var hwnd = FindChatWindow();
        if (hwnd != IntPtr.Zero)
        {
            if (IsWindowVisible(hwnd) && !IsIconic(hwnd))
            {
                ShowWindow(hwnd, 0);
                return;
            }
            ShowAndPosition(hwnd);
            return;
        }

        _tracker.CaptureLatest();
        Directory.CreateDirectory(_profile);
        var restoreUrl = _tracker.GetRestoreUrl();
        Process.Start(new ProcessStartInfo(_edge)
        {
            Arguments = $"--user-data-dir=\"{_profile}\" --app=\"{restoreUrl}\" --new-window --no-first-run",
            WorkingDirectory = Path.GetDirectoryName(_edge)!,
            UseShellExecute = false
        });
        for (var i = 0; i < 100; i++)
        {
            await Task.Delay(150);
            hwnd = FindChatWindow();
            if (hwnd == IntPtr.Zero) continue;
            ShowAndPosition(hwnd);
            PetLog.Info("ChatGPT shown at top-right.");
            return;
        }
        throw new TimeoutException("ChatGPT window could not be located after launching Edge.");
    }

    private IntPtr FindChatWindow()
    {
        if (_window != IntPtr.Zero && IsWindow(_window)) return _window;
        _window = IntPtr.Zero;
        var pids = new HashSet<uint>();
        foreach (var process in Process.GetProcessesByName("msedge"))
        {
            try
            {
                using var query = new System.Management.ManagementObjectSearcher(
                    $"SELECT CommandLine FROM Win32_Process WHERE ProcessId={process.Id}");
                foreach (System.Management.ManagementObject item in query.Get())
                {
                    var cmd = item["CommandLine"]?.ToString() ?? "";
                    if (cmd.Contains(_profile, StringComparison.OrdinalIgnoreCase)
                        && !cmd.Contains("--type=", StringComparison.OrdinalIgnoreCase))
                        pids.Add((uint)process.Id);
                }
            }
            catch { }
            finally { process.Dispose(); }
        }
        EnumWindows((h, _) =>
        {
            GetWindowThreadProcessId(h, out var pid);
            if (!pids.Contains(pid)) return true;
            var title = new StringBuilder(256);
            GetWindowText(h, title, title.Capacity);
            if (!title.ToString().Contains("ChatGPT", StringComparison.OrdinalIgnoreCase))
                return true;
            _window = h;
            return false;
        }, IntPtr.Zero);
        return _window;
    }

    private static void ShowAndPosition(IntPtr hwnd)
    {
        ShowWindow(hwnd, IsIconic(hwnd) ? 9 : 5);
        var monitor = MonitorFromWindow(hwnd, 2);
        var info = new MonitorInfo { cbSize = Marshal.SizeOf<MonitorInfo>() };
        if (GetMonitorInfo(monitor, ref info))
        {
            var work = info.rcWork;
            var width = Math.Min(600, work.Right - work.Left);
            var height = Math.Min(800, work.Bottom - work.Top);
            SetWindowPos(hwnd, IntPtr.Zero, work.Right - width, work.Top,
                width, height, 0x0004 | 0x0010);
        }
        SetWindowPos(hwnd, new IntPtr(-1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0040);
        SetWindowPos(hwnd, new IntPtr(-2), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0040);
        SetForegroundWindow(hwnd);
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
    private delegate bool Callback(IntPtr h, IntPtr param);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr GetProp(IntPtr hwnd, string name);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool SetProp(IntPtr hwnd, string name, IntPtr value);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Callback callback, IntPtr param);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr h, StringBuilder text, int max);
    [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr h);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr h);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr h, int command);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr h, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int width, int height, uint flags);
}
