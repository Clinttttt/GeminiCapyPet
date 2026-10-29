using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace GeminiCapyPet;

public partial class PetWindow : Window
{
    private readonly SpriteAnimator _animator;
    private readonly GeminiWindowController _gemini = new();
    private readonly ChatGptLauncher _chatGpt = new();
    private readonly PetPreferences _preferences = PetPreferences.Load();
    private readonly DispatcherTimer _idleTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DispatcherTimer _pointerTimer = new() { Interval = TimeSpan.FromMilliseconds(20) };

    private Forms.NotifyIcon? _trayIcon;
    private Forms.ContextMenuStrip? _menu;
    private Forms.ToolStripMenuItem? _poseMenu;
    private Forms.ToolStripMenuItem? _sizeMenu;
    private Forms.ToolStripMenuItem? _autoSleepMenu;
    private PetSettingsWindow? _settingsWindow;
    private DateTime _lastActivity = DateTime.UtcNow;
    private Drawing.Point _mouseStart;
    private double _windowStartLeft;
    private double _windowStartTop;
    private bool _pressed;
    private bool _dragging;
    private bool _busy;
    private enum ChatProvider { Gemini, ChatGpt }
    private ChatProvider _activeProvider = ChatProvider.Gemini;

    private Task ToggleActiveChatAsync() => _activeProvider == ChatProvider.ChatGpt
        ? ToggleChatGptAsync() : ToggleGeminiAsync();

    internal PetPose SelectedPose => _animator.Pose;
    internal double PetSize => Width;
    internal bool AutoSleep => _preferences.AutoSleep;
    internal BitmapSource CurrentPoseFrame => _animator.CurrentFrame;

    public PetWindow()
    {
        InitializeComponent();
        _animator = new SpriteAnimator(PetSprite);
        _idleTimer.Tick += OnIdleTick;
        _pointerTimer.Tick += OnPointerTick;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _animator.SetPose(_preferences.SelectedPose);
        ApplySize(_preferences.Size, false);

        if (_preferences.Left is double left && _preferences.Top is double top)
            MoveOnScreen(left, top);
        else
            ResetPetPosition();

        PetLog.Info($"Pet loaded at {Left}, {Top}; size={Width}; pose={SelectedPose}");
        SetupTray();
        _idleTimer.Start();
    }

    private void MoveOnScreen(double left, double top)
    {
        Left = Math.Clamp(left, SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - Width);
        Top = Math.Clamp(top, SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - Height);
    }

    internal void ResetPetPosition()
    {
        MoveOnScreen(SystemParameters.WorkArea.Right - Width - 28,
            SystemParameters.WorkArea.Bottom - Height - 12);
        SavePosition();
    }

    internal void SaveCurrentPreferences()
    {
        _preferences.Size = Width;
        _preferences.Pose = SelectedPose.ToString();
        SavePosition();
    }

    private void SavePosition()
    {
        _preferences.Left = Left;
        _preferences.Top = Top;
        _preferences.Save();
    }

    internal void SetPose(PetPose pose)
    {
        _animator.SetPose(pose);
        _preferences.Pose = pose.ToString();
        _lastActivity = DateTime.UtcNow;
        _preferences.Save();
        UpdateMenuSelections();
        PetLog.Info($"Selected pose: {pose}");
    }

    internal void SetAutoSleep(bool enabled)
    {
        _preferences.AutoSleep = enabled;
        _lastActivity = DateTime.UtcNow;
        _preferences.Save();
        UpdateMenuSelections();
    }

    internal void SetSize(double requestedSize)
    {
        ApplySize(requestedSize, true);
        _preferences.Size = Width;
        SavePosition();
        UpdateMenuSelections();
        PetLog.Info($"Selected pet size: {Width:0}");
    }

    private void ApplySize(double requestedSize, bool preserveBottomRight)
    {
        var size = Math.Clamp(Math.Round(requestedSize / 5) * 5, 140, 360);
        var right = Left + Width;
        var bottom = Top + Height;

        Width = size;
        Height = size + 14;
        PetSprite.Width = size - 4;
        PetSprite.Height = size - 4;
        HintPill.Visibility = size < 180 ? Visibility.Collapsed : Visibility.Visible;

        if (preserveBottomRight)
            MoveOnScreen(right - Width, bottom - Height);
    }

    private void SetupTray()
    {
        _menu = new Forms.ContextMenuStrip();
        _menu.Items.Add("Show / hide Gemini", null, async (_, _) => await ToggleGeminiAsync());
        _menu.Items.Add("Show / hide ChatGPT", null, async (_, _) => await ToggleChatGptAsync());
        _menu.Items.Add(new Forms.ToolStripSeparator());
        _menu.Items.Add("Customize pet...", null, (_, _) => OpenSettings());

        _poseMenu = new Forms.ToolStripMenuItem("Choose pose");
        foreach (var pose in Enum.GetValues<PetPose>())
        {
            var label = pose switch
            {
                PetPose.Automatic => "Automatic (sleep / wake)",
                PetPose.LyingDown => "Lying down",
                _ => pose.ToString()
            };
            var choice = new Forms.ToolStripMenuItem(label) { Tag = pose };
            choice.Click += (_, _) => SetPose(pose);
            _poseMenu.DropDownItems.Add(choice);
        }
        _menu.Items.Add(_poseMenu);

        _sizeMenu = new Forms.ToolStripMenuItem("Pet size");
        AddSizeOption("Small", 150);
        AddSizeOption("Medium", 220);
        AddSizeOption("Large", 290);
        AddSizeOption("Extra large", 360);
        _sizeMenu.DropDownItems.Add(new Forms.ToolStripSeparator());
        _sizeMenu.DropDownItems.Add("Custom size...", null, (_, _) => OpenSettings());
        _menu.Items.Add(_sizeMenu);

        _autoSleepMenu = new Forms.ToolStripMenuItem("Auto-sleep after 45 seconds")
        {
            CheckOnClick = true,
            Checked = _preferences.AutoSleep
        };
        _autoSleepMenu.CheckedChanged += (_, _) =>
        {
            if (_preferences.AutoSleep != _autoSleepMenu.Checked)
                SetAutoSleep(_autoSleepMenu.Checked);
        };
        _menu.Items.Add(_autoSleepMenu);

        _menu.Items.Add("Play sleep animation", null, (_, _) =>
        {
            SetPose(PetPose.Automatic);
            _animator.Sleep();
        });
        _menu.Items.Add("Reset pet position", null, (_, _) => ResetPetPosition());
        _menu.Items.Add(new Forms.ToolStripSeparator());
        _menu.Items.Add("Exit Gemini Capy Pet", null, (_, _) => Close());

        UpdateMenuSelections();

        _trayIcon = new Forms.NotifyIcon
        {
            Text = "Capies - Click to toggle selected chat",
            Icon = Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!)
                ?? Drawing.SystemIcons.Application,
            Visible = true,
            ContextMenuStrip = _menu
        };
        _trayIcon.DoubleClick += (_, _) =>
        {
            Show();
            Activate();
            _animator.Wake();
        };
    }

    private void AddSizeOption(string label, double size)
    {
        var item = new Forms.ToolStripMenuItem($"{label} ({size:0} px)") { Tag = size };
        item.Click += (_, _) => SetSize(size);
        _sizeMenu!.DropDownItems.Add(item);
    }

    private void UpdateMenuSelections()
    {
        if (_poseMenu != null)
        {
            foreach (Forms.ToolStripItem item in _poseMenu.DropDownItems)
                if (item is Forms.ToolStripMenuItem option && option.Tag is PetPose pose)
                    option.Checked = pose == SelectedPose;
        }

        if (_sizeMenu != null)
        {
            foreach (Forms.ToolStripItem item in _sizeMenu.DropDownItems)
                if (item is Forms.ToolStripMenuItem option && option.Tag is double size)
                    option.Checked = Math.Abs(size - PetSize) < 0.1;
        }

        if (_autoSleepMenu != null)
        {
            _autoSleepMenu.Checked = _preferences.AutoSleep;
            _autoSleepMenu.Enabled = SelectedPose == PetPose.Automatic;
        }
    }

    private void OpenSettings()
    {
        PetLog.Info("Opening pet customization window");
        if (_settingsWindow != null)
        {
            _settingsWindow.RefreshFromPet();
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new PetSettingsWindow(this);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    private void OnIdleTick(object? sender, EventArgs e)
    {
        if (_preferences.AutoSleep && SelectedPose == PetPose.Automatic &&
            !_pressed && !_busy && !_gemini.IsVisible && !_chatGpt.IsVisible &&
            DateTime.UtcNow - _lastActivity > TimeSpan.FromSeconds(45))
            _animator.Sleep();
    }

    private void OnPetEnter(object sender, MouseEventArgs e)
    {
        _lastActivity = DateTime.UtcNow;
        HintPill.Opacity = 0.95;
        HintText.Text = _activeProvider == ChatProvider.ChatGpt
            ? "Click to toggle ChatGPT" : "Click to toggle Gemini";
        _animator.Wake();
    }

    private void OnPetLeave(object sender, MouseEventArgs e)
    {
        if (!_busy) HintPill.Opacity = 0;
    }

    private void OnPetMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        PetLog.Info("Pet MouseDown");
        _lastActivity = DateTime.UtcNow;
        _pressed = true;
        _dragging = false;
        _mouseStart = Forms.Control.MousePosition;
        _windowStartLeft = Left;
        _windowStartTop = Top;
        _pointerTimer.Start();
        e.Handled = true;
    }

    private void OnPetMouseMove(object sender, MouseEventArgs e) => UpdateDragPosition();

    private void OnPointerTick(object? sender, EventArgs e)
    {
        if (!_pressed) { _pointerTimer.Stop(); return; }
        UpdateDragPosition();
        if ((Forms.Control.MouseButtons & Forms.MouseButtons.Left) == 0)
            CompleteGesture();
    }

    private void UpdateDragPosition()
    {
        if (!_pressed) return;
        var cursor = Forms.Control.MousePosition;
        var dx = cursor.X - _mouseStart.X;
        var dy = cursor.Y - _mouseStart.Y;
        if (!_dragging && Math.Sqrt(dx * dx + dy * dy) > 7)
            _dragging = true;
        if (!_dragging) return;

        var dpi = VisualTreeHelper.GetDpi(this);
        var x = _windowStartLeft + dx / dpi.DpiScaleX;
        var y = _windowStartTop + dy / dpi.DpiScaleY;
        MoveOnScreen(x, y);
        _lastActivity = DateTime.UtcNow;
    }

    private void OnPetMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        CompleteGesture();
        e.Handled = true;
    }

    private void CompleteGesture()
    {
        if (!_pressed) return;
        _pressed = false;
        _pointerTimer.Stop();
        PetLog.Info($"Pet released; dragging={_dragging}");
        if (_dragging)
        {
            SavePosition();
            return;
        }
        _animator.Wake();
        _ = ToggleActiveChatAsync();
    }

    private void OnPetRightClick(object sender, MouseButtonEventArgs e)
    {
        PetLog.Info("Pet right-click menu requested");
        _menu?.Show(Forms.Control.MousePosition);
        e.Handled = true;
    }

    private async Task ToggleChatGptAsync()
    {
        if (_busy) return;
        _busy = true;
        _activeProvider = ChatProvider.ChatGpt;
        try
        {
            if (_chatGpt.IsVisible) _chatGpt.Hide();
            else
            {
                _gemini.Hide();
                await _chatGpt.ToggleAsync();
            }
        }
        catch (Exception ex)
        {
            PetLog.Info("ChatGPT toggle failed: " + ex);
            System.Windows.MessageBox.Show(this, ex.Message, "Capies - ChatGPT", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally { _busy = false; }
    }

    private async Task ToggleGeminiAsync()
    {
        if (_busy) return;
        _busy = true;
        _activeProvider = ChatProvider.Gemini;
        PetLog.Info("Starting Gemini toggle");
        HintPill.Opacity = 0.95;
        HintText.Text = "Opening Gemini...";
        try
        {
            if (!_gemini.IsVisible) _chatGpt.Hide();
            var visible = await _gemini.ToggleAsync();
            PetLog.Info($"Gemini toggle successful; visible={visible}");
            HintText.Text = visible ? "Gemini opened" : "Gemini hidden";
        }
        catch (Exception exception)
        {
            PetLog.Info($"Gemini error: {exception}");
            HintText.Text = "Gemini unavailable";
            System.Windows.MessageBox.Show(this, exception.Message, "Gemini Capy Pet",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            _busy = false;
            _lastActivity = DateTime.UtcNow;
        }
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        _idleTimer.Stop();
        _pointerTimer.Stop();
        _settingsWindow?.Close();
        _gemini.StopTracking();
        _chatGpt.StopTracking();
        _animator.Dispose();
        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }
        _preferences.Size = Width;
        _preferences.Left = Left;
        _preferences.Top = Top;
        _preferences.Pose = SelectedPose.ToString();
        _preferences.Save();
        _menu?.Dispose();
    }
}

