using GeminiCapyPet;
using Xunit;

namespace Capies.Characterization.Tests;

public sealed class ConversationUrlTests
{
    private const string ConversationId = "6e3c6ee1-bc2d-4d44-bfa8-8e23cc03ab39";

    [Fact]
    public void ChatGptPersonalConversation_StripsQueryAndFragment()
    {
        var accepted = ChatGptConversationTracker.TryNormalize(
            $"https://chatgpt.com/c/{ConversationId}?model=gpt#section", out var url);

        Assert.True(accepted);
        Assert.Equal($"https://chatgpt.com/c/{ConversationId}", url);
    }

    [Fact]
    public void ChatGptProjectConversation_RetainsProjectPath()
    {
        var expected = $"https://chatgpt.com/g/g-project-slug/c/{ConversationId}";
        Assert.True(ChatGptConversationTracker.TryNormalize(expected + "?foo=bar", out var url));
        Assert.Equal(expected, url);
    }

    [Theory]
    [InlineData("http://chatgpt.com/c/6e3c6ee1-bc2d-4d44-bfa8-8e23cc03ab39")]
    [InlineData("https://chatgpt.com.evil.example/c/6e3c6ee1-bc2d-4d44-bfa8-8e23cc03ab39")]
    [InlineData("https://chatgpt.com/c/not-a-guid")]
    [InlineData("https://chatgpt.com/")]
    [InlineData("not a URL")]
    public void InvalidConversation_DoesNotBecomeRestoreUrl(string input)
    {
        Assert.False(ChatGptConversationTracker.TryNormalize(input, out var url));
        Assert.Equal(string.Empty, url);
    }
}
