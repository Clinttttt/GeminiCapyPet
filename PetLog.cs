using System.IO;

namespace GeminiCapyPet;

internal static class PetLog
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GeminiCapyPet", "pet.log");

    public static void Info(string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.AppendAllText(FilePath, $"{DateTime.Now:O} | {message}{Environment.NewLine}");
        }
        catch { /* Diagnostics must never interrupt the pet. */ }
    }
}