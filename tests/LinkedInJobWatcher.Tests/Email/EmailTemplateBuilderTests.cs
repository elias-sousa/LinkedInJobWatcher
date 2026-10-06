using FluentAssertions;
using LinkedInJobWatcher.Core.Models;
using LinkedInJobWatcher.Infrastructure.Email;

namespace LinkedInJobWatcher.Tests.Email;

public class EmailTemplateBuilderTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static ScoredItem CreateScored(string title, string company, int score, ContactInfo? contact = null)
    {
        var job = new JobItem
        {
            ExternalId = Guid.NewGuid().ToString(),
            Url = "https://www.linkedin.com/jobs/view/123/",
            Title = title,
            Company = company,
            FullText = "Vaga para " + title + " na " + company + ", remoto, C#, .NET Core.",
            RelativeDateText = "3h",
            PublishedAt = Now - TimeSpan.FromHours(3),
            SimplifiedApplication = true
        };

        var scored = new ScoredItem { Item = job, Score = score, Contact = contact, ContentHash = "hash" };
        scored.Reasons.Add("Foco em .NET/C# (+35)");
        return scored;
    }

    [Fact]
    public void BuildNewItemsEmail_SingleItem_SubjectMatchesExpectedFormat()
    {
        var items = new List<ScoredItem> { CreateScored("Desenvolvedor .NET Senior", "Acme", 92) };

        var (subject, _, _) = EmailTemplateBuilder.BuildNewItemsEmail(items);

        subject.Should().Be("[Vagas .NET] 1 nova vaga – melhor match: 92 pts");
    }

    [Fact]
    public void BuildNewItemsEmail_MultipleItems_SubjectUsesBestScoreAndPluralForm()
    {
        var items = new List<ScoredItem>
        {
            CreateScored("Desenvolvedor .NET Senior", "Acme", 92),
            CreateScored("Backend .NET Senior", "Beta", 70)
        };

        var (subject, _, _) = EmailTemplateBuilder.BuildNewItemsEmail(items);

        subject.Should().Be("[Vagas .NET] 2 novas vagas – melhor match: 92 pts");
    }

    [Fact]
    public void BuildNewItemsEmail_Html_ContainsJobTitleCompanyAndLink()
    {
        var items = new List<ScoredItem> { CreateScored("Desenvolvedor .NET Senior", "Acme Tecnologia", 85) };

        var (_, html, text) = EmailTemplateBuilder.BuildNewItemsEmail(items);

        html.Should().Contain("Desenvolvedor .NET Senior");
        html.Should().Contain("Acme Tecnologia");
        html.Should().Contain("https://www.linkedin.com/jobs/view/123/");
        html.Should().Contain("Candidatura simplificada");
        text.Should().Contain("Desenvolvedor .NET Senior");
    }

    [Fact]
    public void BuildNewItemsEmail_WithContactEmail_IncludesMailtoLink()
    {
        var contact = new ContactInfo { Email = "recrutador@acme.com.br", ContactName = "Fulana" };
        var items = new List<ScoredItem> { CreateScored("Desenvolvedor .NET Senior", "Acme", 85, contact) };

        var (_, html, _) = EmailTemplateBuilder.BuildNewItemsEmail(items);

        html.Should().Contain("mailto:recrutador%40acme.com.br");
        html.Should().Contain("Responder por e-mail");
    }

    [Fact]
    public void BuildNewItemsEmail_WithoutContactEmail_DoesNotIncludeMailtoLink()
    {
        var items = new List<ScoredItem> { CreateScored("Desenvolvedor .NET Senior", "Acme", 85) };

        var (_, html, _) = EmailTemplateBuilder.BuildNewItemsEmail(items);

        html.Should().NotContain("mailto:");
    }

    [Fact]
    public void BuildNewItemsEmail_ItemsAreOrderedByScoreDescending()
    {
        var items = new List<ScoredItem>
        {
            CreateScored("Vaga Baixa", "Beta", 61),
            CreateScored("Vaga Alta", "Acme", 95)
        };

        var (_, html, _) = EmailTemplateBuilder.BuildNewItemsEmail(items);

        html.IndexOf("Vaga Alta", StringComparison.Ordinal).Should().BeLessThan(html.IndexOf("Vaga Baixa", StringComparison.Ordinal));
    }
}
