using GeminiCapyPet;
using Xunit;

namespace Capies.Characterization.Tests;

public sealed class GeminiUrlTests
{
    [Theory]
    [InlineData("https://gemini.google.com/app/a1b2c3?utm=source#top", "https://gemini.google.com/app/a1b2c3")]
    [InlineData(" https://gemini.google.com/app/example ", "https://gemini.google.com/app/example")]
    public void ExistingGeminiConversation_StripsQueryOrWhitespace(string input, string expected)
    {
        Assert.True(GeminiWindowController.TryNormalizeSavedConversationUrl(input, out var url));
        Assert.Equal(expected, url);
    }

    [Theory]
    [InlineData("https://gemini.google.com/app")]
    [InlineData("http://gemini.google.com/app/example")]
    [InlineData("https://gemini.google.com.evil.example/app/example")]
    [InlineData("https://gemini.google.com/other/example")]
    [InlineData("invalid")]
    public void InvalidGeminiConversation_IsRejected(string input)
    {
        Assert.False(GeminiWindowController.TryNormalizeSavedConversationUrl(input, out var url));
        Assert.Equal(string.Empty, url);
    }
}
