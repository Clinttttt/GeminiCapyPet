using System.Windows.Media;
using System.Windows.Interop;
using System.Windows;
using Drawing = System.Drawing;
namespace GeminiCapyPet;
internal static class SoftwareIcons
{
    private static readonly Dictionary<string, ImageSource> Cache = new();
    public static ImageSource Get(SoftwareEntry app)
    {
        if (Cache.TryGetValue(app.Id, out var image)) return image;
        if (app.BuiltIn)
        {
            var drawing = new DrawingGroup();
            var color = app.Id == "gemini" ? System.Windows.Media.Color.FromRgb(153, 167, 255) : System.Windows.Media.Color.FromRgb(120, 218, 183);
            var shape = app.Id == "gemini"
                ? Geometry.Parse("M16,1 Q19,13 31,16 Q19,19 16,31 Q13,19 1,16 Q13,13 16,1 Z")
                : Geometry.Parse("M5,3 L27,3 Q30,3 30,6 L30,22 Q30,25 27,25 L13,25 L5,31 L5,25 Q2,25 2,22 L2,6 Q2,3 5,3 Z");
            drawing.Children.Add(new GeometryDrawing(new SolidColorBrush(color), null, shape));
            if (app.Id == "chatgpt")
                foreach (var x in new[] { 9d, 16d, 23d })
                    drawing.Children.Add(new GeometryDrawing(System.Windows.Media.Brushes.DarkSlateGray, null, new EllipseGeometry(new System.Windows.Point(x,14),1.5,1.5)));
            image = new DrawingImage(drawing); image.Freeze(); Cache[app.Id] = image; return image;
        }
        using var icon = app.Path != null ? TryIcon(app.Path) : (Drawing.Icon)(app.Id == "gemini" ? Drawing.SystemIcons.Information : Drawing.SystemIcons.Application).Clone();
        image = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromWidthAndHeight(32, 32));
        image.Freeze(); Cache[app.Id] = image;
        return image;
    }
    private static Drawing.Icon TryIcon(string path)
    {
        try { return Drawing.Icon.ExtractAssociatedIcon(path) ?? (Drawing.Icon)Drawing.SystemIcons.Application.Clone(); }
        catch { return (Drawing.Icon)Drawing.SystemIcons.Application.Clone(); }
    }
}
