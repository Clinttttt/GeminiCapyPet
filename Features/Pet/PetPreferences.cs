using System.IO;
using System.Text.Json;

namespace GeminiCapyPet;

internal sealed class PetPreferences
{
    public string Pose { get; set; } = nameof(PetPose.Automatic);
    public double Size { get; set; } = 220;
    public bool AutoSleep { get; set; } = true;
    public string ActiveChatProvider { get; set; } = "Gemini";
    public double? Left { get; set; }
    public double? Top { get; set; }

    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GeminiCapyPet", "settings.json");

    public static PetPreferences Load() => LoadFrom(SettingsPath);

    // An explicit path lets characterization tests exercise the real serializer
    // without reading or writing the user's live preferences.
    internal static PetPreferences LoadFrom(string path)
    {
        try
        {
            if (!File.Exists(path)) return new();
            var loaded = JsonSerializer.Deserialize<PetPreferences>(File.ReadAllText(path)) ?? new();
            loaded.Size = Math.Clamp(loaded.Size, 140, 360);
            if (!Enum.TryParse<PetPose>(loaded.Pose, out _))
                loaded.Pose = nameof(PetPose.Automatic);
            return loaded;
        }
        catch (Exception ex)
        {
            PetLog.Info("Could not read settings: " + ex.Message);
            return new();
        }
    }

    [System.Text.Json.Serialization.JsonIgnore]
    public PetPose SelectedPose =>
        Enum.TryParse<PetPose>(Pose, out var pose) ? pose : PetPose.Automatic;

    public void Save() => SaveTo(SettingsPath);

    internal void SaveTo(string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(this,
                new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            PetLog.Info("Could not save settings: " + ex.Message);
        }
    }
}
