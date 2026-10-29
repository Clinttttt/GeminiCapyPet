using System.IO;
using System.Text.Json;

namespace GeminiCapyPet;

internal sealed class PetPreferences
{
    public string Pose { get; set; } = nameof(PetPose.Automatic);
    public double Size { get; set; } = 220;
    public bool AutoSleep { get; set; } = true;
    public double? Left { get; set; }
    public double? Top { get; set; }

    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GeminiCapyPet", "settings.json");

    public static PetPreferences Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return new();
            var loaded = JsonSerializer.Deserialize<PetPreferences>(File.ReadAllText(SettingsPath)) ?? new();
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

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this,
                new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            PetLog.Info("Could not save settings: " + ex.Message);
        }
    }
}
