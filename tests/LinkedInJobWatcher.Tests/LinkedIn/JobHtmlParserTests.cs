using FluentAssertions;
using LinkedInJobWatcher.Infrastructure.LinkedIn;

namespace LinkedInJobWatcher.Tests.LinkedIn;

public class JobHtmlParserTests
{
    private readonly JobHtmlParser _parser = new();

    private static string ReadFixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    [Fact]
    public async Task ParseCardAsync_JobCard_ExtractsAllFields()
    {
        var html = ReadFixture("job_card_senior_dotnet.html");

        var job = await _parser.ParseCardAsync(html, ".NET Senior");

        job.Should().NotBeNull();
        job!.ExternalId.Should().Be("4123456789");
        job.Title.Should().Be("Desenvolvedor .NET Senior");
        job.Company.Should().Be("Acme Tecnologia");
        job.Location.Should().Be("Brasil (Remoto)");
        job.SimplifiedApplication.Should().BeTrue();
        job.RelativeDateText.Should().Be("1 dia atrás");
        job.Url.Should().Contain("4123456789");
        job.SearchTerm.Should().Be(".NET Senior");
    }

    [Fact]
    public async Task EnrichWithDetailsAsync_AddsFullDescriptionAndRecruiter()
    {
        var cardHtml = ReadFixture("job_card_senior_dotnet.html");
        var detailsHtml = ReadFixture("job_details_senior_dotnet.html");

        var job = await _parser.ParseCardAsync(cardHtml, ".NET Senior");
        await _parser.EnrichWithDetailsAsync(job!, detailsHtml);

        job!.FullText.Should().Contain("Estamos contratando um Desenvolvedor .NET Senior");
        job.FullText.Should().Contain("PIX");
        job.RecruiterName.Should().Be("Carla Talent Acquisition");
    }

    [Fact]
    public async Task ParseCardAsync_MissingJobId_ReturnsNull()
    {
        const string html = "<li class=\"no-id\"><a class=\"job-card-list__title\" href=\"/jobs/view/1\">Vaga</a></li>";

        var job = await _parser.ParseCardAsync(html, "termo");

        job.Should().BeNull();
    }
}
