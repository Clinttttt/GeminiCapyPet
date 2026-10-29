namespace GeminiCapyPet;

internal interface IManagedApp
{
    bool Visible { get; }
    Task ShowAsync();
    void Hide();
    void Release() { }
}
internal sealed class DelegateManagedApp(Func<bool> visible, Func<Task> show, Action hide) : IManagedApp
{
    public bool Visible => visible();
    public Task ShowAsync() => show();
    public void Hide() => hide();
}
/// <summary>Serializes commands without blocking the UI; legacy adapters remain isolated during extraction.</summary>
internal sealed class AppCoordinator
{
    private readonly SemaphoreSlim _commands = new(1);
    private readonly Dictionary<string, IManagedApp> _apps = new();
    private readonly Func<SoftwareEntry, IManagedApp> _factory;
    private int _pending;
    private bool _stopping;
    public bool Busy => Volatile.Read(ref _pending) != 0;
    public bool AnyVisible { get { lock (_apps) return _apps.Values.Any(a => a.Visible); } }
    public AppCoordinator(Func<SoftwareEntry, IManagedApp> factory) => _factory = factory;
    public async Task ToggleAsync(SoftwareEntry entry, bool activate = false)
    {
        Interlocked.Increment(ref _pending);
        await _commands.WaitAsync();
        try
        {
            if (_stopping) return;
            IManagedApp app;
            lock (_apps)
            {
                if (!_apps.TryGetValue(entry.Id, out app!)) _apps[entry.Id] = app = _factory(entry);
            }
            await Task.Run(async () =>
            {
                if (!activate && app.Visible) { app.Hide(); return; }
                await app.ShowAsync();
                IManagedApp[] others;
                lock (_apps) others = _apps.Values.Where(a => a != app).ToArray();
                foreach (var other in others) if (other.Visible) other.Hide();
            });
        }
        finally { _commands.Release(); Interlocked.Decrement(ref _pending); }
    }
    public async Task ReleaseAsync(string id)
    {
        await _commands.WaitAsync();
        try
        {
            IManagedApp? app;
            lock (_apps) { _apps.Remove(id, out app); }
            if (app != null) await Task.Run(app.Release);
        }
        finally { _commands.Release(); }
    }
    public async Task StopAsync()
    {
        _stopping = true;
        await _commands.WaitAsync();
        try { IManagedApp[] apps; lock (_apps) apps = _apps.Values.ToArray(); await Task.Run(() => { foreach (var app in apps) app.Release(); }); }
        finally { _commands.Release(); }
    }
}
