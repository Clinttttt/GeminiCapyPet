using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GeminiCapyPet;

/// <summary>Native menu behavior with a lightweight, translucent charcoal surface.</summary>
internal sealed class GlassContextMenuStrip : ContextMenuStrip
{
    private readonly Font _menuFont = new("Segoe UI", 10f);
    private readonly ToolStripRenderer _glassRenderer = new GlassMenuRenderer();
    private readonly ToolStripRenderer _accessibleRenderer = new ToolStripSystemRenderer();
    private bool _initialized;

    public GlassContextMenuStrip()
    {
        Font = _menuFont;
        AccessibleName = "Capies menu";
        ShowImageMargin = false;
        ShowCheckMargin = true;
        DropShadowEnabled = true;
    }

    // Call after adding items. Events are attached once, never on the opening path.
    public void InitializeTheme()
    {
        if (_initialized) return;
        _initialized = true;
        Configure(this);
    }

    private void Configure(ToolStripDropDown menu)
    {
        ApplyTheme(menu);
        menu.Opening += (_, _) => ApplyTheme(menu);
        menu.HandleCreated += (_, _) => ApplyWindowStyle(menu);
        foreach (ToolStripItem item in menu.Items)
            if (item is ToolStripMenuItem choice && choice.HasDropDownItems)
                Configure(choice.DropDown);
    }

    public void RefreshTheme()
    {
        RefreshTree(this);
    }
    private void RefreshTree(ToolStripDropDown menu)
    {
        ApplyTheme(menu);
        foreach (ToolStripItem item in menu.Items)
            if (item is ToolStripMenuItem choice && choice.HasDropDownItems) RefreshTree(choice.DropDown);
    }

    private void ApplyTheme(ToolStripDropDown menu)
    {
        bool highContrast = SystemInformation.HighContrast;
        menu.Renderer = highContrast ? _accessibleRenderer : _glassRenderer;
        menu.BackColor = highContrast ? SystemColors.Menu : GlassMenuRenderer.Surface;
        menu.ForeColor = highContrast ? SystemColors.MenuText : GlassMenuRenderer.Text;
        menu.Font = _menuFont;
        // Uniform translucency uses ToolStripDropDown itself: no blur capture or timers.
        menu.Opacity = highContrast ? 1.0 : 0.96;
        int Scale(int value) => Math.Max(1, (int)Math.Round(value * menu.DeviceDpi / 96f));
        menu.Padding = new Padding(Scale(6));
        if (menu is ToolStripDropDownMenu dropDown)
        {
            dropDown.ShowImageMargin = false;
            dropDown.ShowCheckMargin = true;
        }
        foreach (ToolStripItem item in menu.Items)
            item.Padding = item is ToolStripSeparator
                ? new Padding(0, Scale(4), 0, Scale(4))
                : new Padding(Scale(4), Scale(6), Scale(8), Scale(6));
        if (menu.IsHandleCreated) ApplyWindowStyle(menu);
    }

    private static void ApplyWindowStyle(ToolStripDropDown menu)
    {
        // Optional Windows 11 rounding; older systems retain a fully usable menu.
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) return;
        int corner = SystemInformation.HighContrast ? 1 : 2;
        _ = DwmSetWindowAttribute(menu.Handle, 33, ref corner, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _menuFont.Dispose();
    }
}

internal sealed class GlassMenuRenderer : ToolStripProfessionalRenderer
{
    internal static readonly Color Surface = Color.FromArgb(27, 31, 39);
    internal static readonly Color Text = Color.FromArgb(237, 241, 247);
    private static readonly Color Muted = Color.FromArgb(151, 159, 173);
    private static readonly Color Accent = Color.FromArgb(154, 210, 255);

    public GlassMenuRenderer() => RoundedEdges = false;
    private static float Scale(Graphics graphics) => graphics.DpiX / 96f;

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        var bounds = new Rectangle(Point.Empty, e.ToolStrip.Size);
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        using var brush = new LinearGradientBrush(bounds,
            Color.FromArgb(35, 40, 50), Color.FromArgb(24, 28, 36), 90f);
        e.Graphics.FillRectangle(brush, bounds);
    }

    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
    {
        // Keep the check column on the same surface as the text.
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        var bounds = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        using var pen = new Pen(Color.FromArgb(75, 84, 101));
        e.Graphics.DrawRectangle(pen, bounds);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (!e.Item.Enabled || !e.Item.Selected) return;
        float scale = Scale(e.Graphics);
        float availableWidth = e.Item.Owner is { } owner
            ? Math.Min(e.Item.Width, owner.ClientSize.Width - e.Item.Bounds.Left)
            : e.Item.Width;
        var bounds = new RectangleF(4 * scale, scale,
            availableWidth - 8 * scale, e.Item.Height - 2 * scale);
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        var state = e.Graphics.Save();
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = RoundedRectangle(bounds, 5 * scale);
        using var fill = new SolidBrush(Color.FromArgb(56, 68, 86));
        using var border = new Pen(Color.FromArgb(87, 109, 137));
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);
        e.Graphics.Restore(state);
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        // ToolStrip's default layout offsets text when item padding is increased.
        e.TextRectangle = new Rectangle(e.TextRectangle.X, 0, e.TextRectangle.Width, e.Item.Height);
        e.TextFormat |= TextFormatFlags.VerticalCenter;
        if (e.Item.Enabled)
        {
            e.TextColor = Text;
            base.OnRenderItemText(e);
        }
        else
        {
            // The base renderer substitutes a low-contrast system gray for disabled text.
            TextRenderer.DrawText(e.Graphics, e.Text, e.TextFont, e.TextRectangle, Muted, e.TextFormat);
        }
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        e.ArrowColor = e.Item?.Enabled == true ? Text : Muted;
        base.OnRenderArrow(e);
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        var state = e.Graphics.Save();
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        float scale = Scale(e.Graphics);
        float x = e.ImageRectangle.Left + e.ImageRectangle.Width / 2f;
        float y = e.Item.Height / 2f;
        using var pen = new Pen(e.Item.Enabled ? Accent : Muted, 2 * scale)
        {
            StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round
        };
        if (e.Item is ToolStripMenuItem { CheckState: CheckState.Indeterminate })
            e.Graphics.DrawLine(pen, x - 4 * scale, y, x + 4 * scale, y);
        else
            e.Graphics.DrawLines(pen, new[]
            {
                new PointF(x - 4 * scale, y),
                new PointF(x - scale, y + 3 * scale),
                new PointF(x + 5 * scale, y - 4 * scale)
            });
        e.Graphics.Restore(state);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        float scale = Scale(e.Graphics);
        using var pen = new Pen(Color.FromArgb(65, 73, 88));
        float y = e.Item.Height / 2f;
        e.Graphics.DrawLine(pen, 8 * scale, y, e.Item.Width - 8 * scale, y);
    }

    private static GraphicsPath RoundedRectangle(RectangleF bounds, float radius)
    {
        var path = new GraphicsPath();
        float diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
