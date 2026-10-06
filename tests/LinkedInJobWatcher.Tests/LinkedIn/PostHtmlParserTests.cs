using FluentAssertions;
using LinkedInJobWatcher.Infrastructure.LinkedIn;

namespace LinkedInJobWatcher.Tests.LinkedIn;

public class PostHtmlParserTests
{
    private readonly PostHtmlParser _parser = new();

    private static string ReadFixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    [Fact]
    public async Task ParseAsync_JobPost_ExtractsAllFields()
    {
        var html = ReadFixture("post_vaga_dotnet_remota.html");

        var post = await _parser.ParseAsync(html, "vaga desenvolvedor .net senior remoto");

        post.Should().NotBeNull();
        post!.ExternalId.Should().Be("urn:li:activity:7123456789001");
        post.AuthorName.Should().Be("Fulana Recrutadora");
        post.AuthorHeadline.Should().Be("Tech Recruiter na Acme Tecnologia");
        post.RelativeDateText.Should().Be("3h");
        post.FullText.Should().Contain("Desenvolvedor .NET Senior");
        post.FullText.Should().Contain("gupy.io");
        post.Url.Should().Contain("urn:li:activity:7123456789001");
        post.SearchTerm.Should().Be("vaga desenvolvedor .net senior remoto");
    }

    [Fact]
    public async Task ParseAsync_NonJobPost_StillParsesButTextHasNoJobIndicator()
    {
        var html = ReadFixture("post_nao_vaga.html");

        var post = await _parser.ParseAsync(html, "vaga desenvolvedor .net senior remoto");

        post.Should().NotBeNull();
        post!.FullText.Should().Contain("fui contratado");
    }

    [Fact]
    public async Task ParseAsync_MissingUrn_ReturnsNull()
    {
        const string html = "<div class=\"no-urn\"><div class=\"update-components-text\">Sem urn</div></div>";

        var post = await _parser.ParseAsync(html, "termo");

        post.Should().BeNull();
    }
}
