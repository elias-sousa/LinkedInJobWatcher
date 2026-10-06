using FluentAssertions;
using LinkedInJobWatcher.Core.Parsing;

namespace LinkedInJobWatcher.Tests.Parsing;

public class RelativeDateParserTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);
    private readonly RelativeDateParser _parser = new();

    [Theory]
    [InlineData("5 min", 5 * 60)]
    [InlineData("5m", 5 * 60)]
    [InlineData("3 h", 3 * 3600)]
    [InlineData("3h", 3 * 3600)]
    [InlineData("1 d", 24 * 3600)]
    [InlineData("1d", 24 * 3600)]
    [InlineData("2 dias", 2 * 24 * 3600)]
    [InlineData("2d", 2 * 24 * 3600)]
    [InlineData("1 sem", 7 * 24 * 3600)]
    [InlineData("1w", 7 * 24 * 3600)]
    [InlineData("há 3 horas", 3 * 3600)]
    [InlineData("Reposted 1d", 24 * 3600)]
    [InlineData("Edited • 2h", 2 * 3600)]
    public void TryParse_RecognizedFormats_ReturnsExpectedOffset(string relativeText, int expectedSecondsAgo)
    {
        var success = _parser.TryParse(relativeText, Now, out var publishedAt);

        success.Should().BeTrue();
        publishedAt.Should().Be(Now - TimeSpan.FromSeconds(expectedSecondsAgo));
    }

    [Fact]
    public void TryParse_ExactlyFortyEightHours_IsStillParsed()
    {
        var success = _parser.TryParse("2 dias", Now, out var publishedAt);

        success.Should().BeTrue();
        (Now - publishedAt).Should().Be(TimeSpan.FromHours(48));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("algum texto sem data")]
    [InlineData("Promoted")]
    public void TryParse_UnrecognizedText_ReturnsFalse(string relativeText)
    {
        var success = _parser.TryParse(relativeText, Now, out _);

        success.Should().BeFalse();
    }

    [Fact]
    public void TryParse_JustNow_ReturnsReferenceInstant()
    {
        var success = _parser.TryParse("agora mesmo", Now, out var publishedAt);

        success.Should().BeTrue();
        publishedAt.Should().Be(Now);
    }
}
