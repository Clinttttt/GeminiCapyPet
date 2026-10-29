using Microsoft.Data.Sqlite;
using System.IO;

namespace GeminiCapyPet;

/// <summary>Read-only discovery of Gemini conversation URLs from the pet's
/// dedicated Edge profile. Never changes Edge's history or session database.</summary>
internal sealed class GeminiConversationTracker
{
    private readonly string _history;
    private readonly string _saved;
    private readonly System.Windows.Threading.DispatcherTimer _timer =
        new() { Interval = TimeSpan.FromSeconds(5) };
    private string? _lastSaved;

    public GeminiConversationTracker(string profileDirectory)
    {
        _history = Path.Combine(profileDirectory, "Default", "History");
        _saved = Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData),
            "GeminiCapyPet", "last-conversation.txt");
        _timer.Tick += (_, _) => CaptureLatest();
    }

    public void Start()
    {
        CaptureLatest();
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
        CaptureLatest();
    }

    public void CaptureLatest()
    {
        try
        {
            if (!File.Exists(_history)) return;
            // Read SQLite in read-only mode; do not lock or modify Edge's database.
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = _history,
                Mode = SqliteOpenMode.ReadOnly,
                Cache = SqliteCacheMode.Private,
                Pooling = false,
                DefaultTimeout = 1
            };
            using var connection = new SqliteConnection(builder.ToString());
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT url FROM urls
                WHERE url LIKE 'https://gemini.google.com/app/%'
                ORDER BY last_visit_time DESC LIMIT 12
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var candidate = reader.GetString(0);
                if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri)
                    || uri.Scheme != "https"
                    || uri.Host != "gemini.google.com"
                    || !uri.AbsolutePath.StartsWith("/app/", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.Length < 12)
                    continue;

                var canonical = uri.GetLeftPart(UriPartial.Path);
                if (canonical == _lastSaved) return;
                Directory.CreateDirectory(Path.GetDirectoryName(_saved)!);
                File.WriteAllText(_saved, canonical);
                _lastSaved = canonical;
                PetLog.Info("Saved most recent Gemini conversation URL.");
                return;
            }
        }
        catch (SqliteException ex)
        {
            // Chromium may temporarily lock the History database. Retry later.
            if (ex.SqliteErrorCode != 5 && ex.SqliteErrorCode != 6)
                PetLog.Info("Conversation tracking unavailable: " + ex.Message);
        }
        catch (Exception ex)
        {
            PetLog.Info("Conversation tracking unavailable: " + ex.Message);
        }
    }
}
