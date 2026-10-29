using System.IO;
using System.Text.Json;
namespace GeminiCapyPet;

internal sealed record SoftwareEntry(string Id, string Name, string? Path = null)
{
    public bool BuiltIn => Path == null;
}
internal sealed record SoftwareDocument(int Version, string SelectedId, SoftwareEntry[] Apps);
internal sealed class SoftwareCatalog
{
    private readonly string _path;
    public List<SoftwareEntry> Apps { get; private set; } = [];
    public string SelectedId { get; private set; }
    public string? Error { get; private set; }
    public SoftwareEntry Selected => Apps.FirstOrDefault(a => a.Id == SelectedId) ?? Apps[0];
    public SoftwareCatalog(string path, string legacy)
    {
        _path = path;
        SelectedId = legacy.Equals("ChatGpt", StringComparison.OrdinalIgnoreCase) ? "chatgpt" : "gemini";
        Apps = [new("gemini", "Gemini"), new("chatgpt", "ChatGPT")];
        if (!File.Exists(path)) return;
        try
        {
            var doc = JsonSerializer.Deserialize<SoftwareDocument>(File.ReadAllText(path)) ?? throw new IOException("Empty library.");
            if (doc.Version != 1 || doc.Apps == null) throw new IOException("Unsupported library format.");
            var custom = doc.Apps.Where(a => !a.BuiltIn).ToArray();
            if (custom.Any(a => string.IsNullOrWhiteSpace(a.Name) || string.IsNullOrWhiteSpace(a.Id)) ||
                Apps.Concat(custom).Select(a => a.Id).Distinct().Count() != Apps.Count + custom.Length)
                throw new IOException("Invalid software entries.");
            Apps.AddRange(custom);
            SelectedId = Apps.Any(a => a.Id == doc.SelectedId) ? doc.SelectedId : SelectedId;
        }
        catch (Exception ex) { Error = "Software library is read-only: " + ex.Message; }
    }
    private void Save(List<SoftwareEntry> apps, string selected)
    {
        if (Error != null) throw new IOException(Error);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_path)!);
        var temporary = _path + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(new SoftwareDocument(1, selected, apps.ToArray()), new JsonSerializerOptions { WriteIndented = true }));
            if (File.Exists(_path)) File.Replace(temporary, _path, _path + ".bak"); else File.Move(temporary, _path);
            Apps = apps; SelectedId = selected;
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
    public void Select(string id)
    {
        if (!Apps.Any(a => a.Id == id)) throw new ArgumentException("Software was removed.");
        if (SelectedId != id) Save([.. Apps], id);
    }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
    public void Add(string path)
    {
        path = System.IO.Path.GetFullPath(path);
        if (!File.Exists(path) || !path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Choose an existing .exe program.");
        if (Apps.Any(a => string.Equals(a.Path, path, StringComparison.OrdinalIgnoreCase))) throw new ArgumentException("This program is already added.");
        var app = new SoftwareEntry(Guid.NewGuid().ToString("N"), System.IO.Path.GetFileNameWithoutExtension(path), path);
        Save([.. Apps, app], app.Id);
    }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
    public void Remove(string id)
    {
        var entry = Apps.First(a => a.Id == id);
        if (entry.BuiltIn) throw new ArgumentException("Built-in apps stay available.");
        Save(Apps.Where(a => a.Id != id).ToList(), SelectedId == id ? "gemini" : SelectedId);
    }
}
