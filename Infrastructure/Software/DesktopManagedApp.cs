using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
namespace GeminiCapyPet;

internal sealed class DesktopManagedApp(SoftwareEntry entry) : IManagedApp
{
    private IntPtr _window;
    private uint _pid;
    private bool _hidden;
    private readonly string _tag = "Capies.Software." + entry.Id;
    private readonly IntPtr _cookie = new(Random.Shared.Next(1, int.MaxValue));
    private bool Alive => _window != IntPtr.Zero && IsWindow(_window) && GetProp(_window, _tag) == _cookie &&
        GetWindowThreadProcessId(_window, out var pid) != 0 && pid == _pid;
    public bool Visible => Alive && IsWindowVisible(_window) && !IsIconic(_window);
    public void Hide() { if (Alive) { ShowWindowAsync(_window, 0); _hidden = true; } }
    public void Release() { if (_hidden && Alive) ShowWindowAsync(_window, 5); _hidden = false; }
    public async Task ShowAsync()
    {
        if (!Alive)
        {
            if (!File.Exists(entry.Path)) throw new FileNotFoundException("This program has moved or was removed. Add its new location.");
            _window = Find();
            if (_window == IntPtr.Zero)
            {
                using var process = Process.Start(new ProcessStartInfo(entry.Path!) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(entry.Path)! });
                var watch = Stopwatch.StartNew();
                while (_window == IntPtr.Zero && watch.Elapsed < TimeSpan.FromSeconds(15))
                {
                    await Task.Delay(150);
                    _window = Find();
                }
            }
            if (_window == IntPtr.Zero) throw new TimeoutException("No compatible window appeared. This program may need a dedicated integration.");
            GetWindowThreadProcessId(_window, out _pid);
            if (!SetProp(_window, _tag, _cookie)) throw new InvalidOperationException("Windows denied access to this software window.");
            Position();
        }
        ShowWindowAsync(_window, IsIconic(_window) ? 9 : 5);
        for (int i = 0; i < 30 && !Visible; i++) await Task.Delay(10);
        if (!Visible) throw new InvalidOperationException("Windows could not restore the app window.");
        _hidden = false;
        SetForegroundWindow(_window);
    }
    private IntPtr Find()
    {
        var matches = new List<IntPtr>();
        EnumWindows((h, _) =>
        {
            if (GetWindow(h, 4) != IntPtr.Zero || GetWindowTextLength(h) == 0) return true;
            GetWindowThreadProcessId(h, out var pid);
            try
            {
                using var process = Process.GetProcessById((int)pid);
                if (string.Equals(process.MainModule?.FileName, entry.Path, StringComparison.OrdinalIgnoreCase)) matches.Add(h);
            }
            catch { }
            return true;
        }, IntPtr.Zero);
        if (matches.Count > 1) throw new InvalidOperationException("Several windows belong to this program. Keep one main window open, then try again.");
        return matches.Count == 1 ? matches[0] : IntPtr.Zero;
    }
    private void Position()
    {
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(MonitorFromWindow(_window, 2), ref info)) return;
        double scale = Math.Max(96, GetDpiForWindow(_window)) / 96d;
        int width = Math.Min((int)(600 * scale), info.Work.Right - info.Work.Left);
        int height = Math.Min((int)(760 * scale), info.Work.Bottom - info.Work.Top);
        if ((GetWindowLong(_window, -16) & 0x40000) == 0 && GetWindowRect(_window, out var bounds))
        { width = bounds.Right - bounds.Left; height = bounds.Bottom - bounds.Top; }
        SetWindowPos(_window, IntPtr.Zero, Math.Max(info.Work.Left, info.Work.Right - width), info.Work.Top, width, height, 0x0004 | 0x0010 | 0x4000);
    }
    private delegate bool Callback(IntPtr h, IntPtr p);
    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public int Size; public Rect Monitor, Work; public uint Flags; }
    [DllImport("user32.dll")] private static extern bool EnumWindows(Callback c, IntPtr p);
    [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr h);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr h);
    [DllImport("user32.dll")] private static extern bool ShowWindowAsync(IntPtr h, int c);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] private static extern IntPtr GetWindow(IntPtr h, uint c);
    [DllImport("user32.dll")] private static extern int GetWindowTextLength(IntPtr h);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] private static extern bool SetProp(IntPtr h, string key, IntPtr value);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] private static extern IntPtr GetProp(IntPtr h, string key);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr h, uint f);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] private static extern bool GetMonitorInfo(IntPtr h, ref MonitorInfo info);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr h);
    [DllImport("user32.dll", EntryPoint="GetWindowLongW")] private static extern int GetWindowLong(IntPtr h, int i);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr h, out Rect r);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr h, IntPtr a, int x, int y, int w, int z, uint f);
}
