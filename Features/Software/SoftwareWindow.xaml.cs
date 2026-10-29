using System.Windows;
using System.Windows.Controls;
namespace GeminiCapyPet;
public partial class SoftwareWindow : Window
{
    private readonly SoftwareCatalog _catalog;
    private readonly Func<string, Task> _use;
    private readonly Func<string, Task> _remove;
    private readonly Action _refresh;
    private sealed record Row(SoftwareEntry App)
    {
        public string Name => App.Name;
        public string Detail => App.Path ?? "Built-in app";
        public System.Windows.Media.ImageSource Icon => SoftwareIcons.Get(App);
    }
    internal SoftwareWindow(SoftwareCatalog catalog, Func<string, Task> use, Func<string, Task> remove, Action refresh)
    {
        InitializeComponent(); _catalog = catalog; _use = use; _remove = remove; _refresh = refresh;
        Reload();
    }
    private void Reload()
    {
        AppList.ItemsSource = _catalog.Apps.Select(a => new Row(a)).ToArray();
        AppList.SelectedIndex = _catalog.Apps.FindIndex(a => a.Id == _catalog.SelectedId);
        Status.Text = _catalog.Error ?? "Added programs open at the top-right. Removing one keeps its files.";
    }
    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    { if (RemoveButton != null) RemoveButton.IsEnabled = AppList.SelectedItem is Row { App.BuiltIn: false }; }
    private async void OnAdd(object sender, RoutedEventArgs e)
    {
        var picker = new Microsoft.Win32.OpenFileDialog { Title = "Add software to Capies", Filter = "Windows programs (*.exe)|*.exe", CheckFileExists = true };
        if (picker.ShowDialog(this) != true) return;
        try { await Task.Run(() => _catalog.Add(picker.FileName)); Reload(); _refresh(); }
        catch (Exception ex) { Status.Text = ex.Message; }
    }
    private async void OnRemove(object sender, RoutedEventArgs e)
    {
        if (AppList.SelectedItem is not Row row) return;
        try { await _remove(row.App.Id); Reload(); _refresh(); }
        catch (Exception ex) { Status.Text = ex.Message; }
    }
    private async void OnUse(object sender, RoutedEventArgs e)
    {
        if (AppList.SelectedItem is not Row row) return;
        try { await _use(row.App.Id); Status.Text = row.Name + " selected."; }
        catch (Exception ex) { Status.Text = ex.Message; }
    }
}
