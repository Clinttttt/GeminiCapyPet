using System.IO;
using System.Text.Json;
using GeminiCapyPet;
using Xunit;

namespace Capies.Characterization.Tests;

public sealed class PetPreferencesTests
{
    [Fact]
    public void AbsentSettings_UsesLegacyDefaults()
    {
        using var fixture = new TemporarySettings();
        var preferences = PetPreferences.LoadFrom(fixture.FilePath);

        Assert.Equal(PetPose.Automatic, preferences.SelectedPose);
        Assert.Equal(220, preferences.Size);
        Assert.True(preferences.AutoSleep);
        Assert.Equal("Gemini", preferences.ActiveChatProvider);
        Assert.Null(preferences.Left);
        Assert.Null(preferences.Top);
        Assert.False(File.Exists(fixture.FilePath));
    }

    [Fact]
    public void SaveAndLoad_RoundTripsLegacyFields()
    {
        using var fixture = new TemporarySettings();
        var expected = new PetPreferences
        {
            Pose = nameof(PetPose.LyingDown), Size = 290, AutoSleep = false,
            ActiveChatProvider = "ChatGpt", Left = 123.5, Top = 76.25
        };
        expected.SaveTo(fixture.FilePath);

        Assert.True(File.Exists(fixture.FilePath));
        var actual = PetPreferences.LoadFrom(fixture.FilePath);
        Assert.Equal(expected.Pose, actual.Pose);
        Assert.Equal(expected.Size, actual.Size);
        Assert.Equal(expected.AutoSleep, actual.AutoSleep);
        Assert.Equal(expected.ActiveChatProvider, actual.ActiveChatProvider);
        Assert.Equal(expected.Left, actual.Left);
        Assert.Equal(expected.Top, actual.Top);

        using var json = JsonDocument.Parse(File.ReadAllText(fixture.FilePath));
        Assert.False(json.RootElement.TryGetProperty("SelectedPose", out _));
    }

    [Theory]
    [InlineData(100, 140)]
    [InlineData(500, 360)]
    public void Load_ClampsLegacySize_AndDefaultsInvalidPose(double saved, double expected)
    {
        using var fixture = new TemporarySettings();
        File.WriteAllText(fixture.FilePath, JsonSerializer.Serialize(new
        {
            Pose = "UnknownPose", Size = saved, AutoSleep = true, ActiveChatProvider = "ChatGpt"
        }));

        var result = PetPreferences.LoadFrom(fixture.FilePath);

        Assert.Equal(expected, result.Size);
        Assert.Equal(PetPose.Automatic, result.SelectedPose);
        Assert.Equal("ChatGpt", result.ActiveChatProvider);
    }

    private sealed class TemporarySettings : IDisposable
    {
        private readonly string _folder = Path.Combine(
            Path.GetTempPath(), "Capies.Characterization", Guid.NewGuid().ToString("N"));
        public TemporarySettings() => Directory.CreateDirectory(_folder);
        public string FilePath => Path.Combine(_folder, "settings.json");
        public void Dispose() => Directory.Delete(_folder, recursive: true);
    }
}
