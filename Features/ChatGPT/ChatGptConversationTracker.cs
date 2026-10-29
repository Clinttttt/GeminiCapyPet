using Microsoft.Data.Sqlite;
using System.IO;
using System.Windows.Threading;

namespace GeminiCapyPet;

/// <summary>Tracks the most recently visited ChatGPT conversation in the dedicated Edge profile.</summary>
internal sealed class ChatGptConversationTracker
{
    private readonly string _history;
    private readonly string _saved;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(5) };
    private string? _lastSaved;

    public ChatGptConversationTracker(string profile)
    {
        _history = Path.Combine(profile, "Default", "History");
        _saved = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GeminiCapyPet", "last-chatgpt-conversation.txt");
        _timer.Tick += (_, _) => CaptureLatest();
    }

    public string GetRestoreUrl()
    {
        try
        {
            if (File.Exists(_saved) && TryNormalize(File.ReadAllText(_saved), out var url))
                return url;
        }
        catch (Exception ex) { PetLog.Info("ChatGPT restore URL read failed: " + ex.Message); }
        return "https://chatgpt.com/";
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
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = _history, Mode = SqliteOpenMode.ReadOnly,
                Cache = SqliteCacheMode.Private, Pooling = false, DefaultTimeout = 1
            };
            using var connection = new SqliteConnection(builder.ToString());
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT url FROM urls
                WHERE url LIKE 'https://chatgpt.com/c/%'
                   OR url LIKE 'https://chatgpt.com/g/%'
                ORDER BY last_visit_time DESC LIMIT 20
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (!TryNormalize(reader.GetString(0), out var url)) continue;
                if (url == _lastSaved) return;
                Directory.CreateDirectory(Path.GetDirectoryName(_saved)!);
                File.WriteAllText(_saved, url);
                _lastSaved = url;
                PetLog.Info("Saved latest ChatGPT conversation URL.");
                return;
            }
        }
        catch (SqliteException ex)
        {
            if (ex.SqliteErrorCode is not (5 or 6))
                PetLog.Info("ChatGPT history unavailable: " + ex.Message);
        }
        catch (Exception ex) { PetLog.Info("ChatGPT tracking unavailable: " + ex.Message); }
    }

    private static bool TryNormalize(string input, out string url)
    {
        url = "";
        if (!Uri.TryCreate(input.Trim(), UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || !uri.Host.Equals("chatgpt.com", StringComparison.OrdinalIgnoreCase))
            return false;
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2) return false;
        if (segments[0] == "c" && Guid.TryParse(segments[1], out _))
        {
            url = $"https://chatgpt.com/c/{segments[1]}";
            return true;
        }
        if (segments[0] == "g" && segments.Length >= 4 && segments[2] == "c"
            && Guid.TryParse(segments[3], out _))
        {
            url = uri.GetLeftPart(UriPartial.Path);
            return true;
        }
        return false;
    }
}
