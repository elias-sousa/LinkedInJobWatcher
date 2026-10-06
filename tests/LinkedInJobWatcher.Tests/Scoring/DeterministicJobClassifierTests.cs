using FluentAssertions;
using LinkedInJobWatcher.Core.Configuration;
using LinkedInJobWatcher.Core.Models;
using LinkedInJobWatcher.Core.Parsing;
using LinkedInJobWatcher.Core.Scoring;
using Microsoft.Extensions.Options;

namespace LinkedInJobWatcher.Tests.Scoring;

public class DeterministicJobClassifierTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);

    private readonly DeterministicJobClassifier _classifier = new(
        Options.Create(new FilterOptions()),
        new ContactExtractor(),
        new FakeTimeProvider(Now));

    private static JobItem CreateJob(string fullText, DateTimeOffset? publishedAt = null, string title = "Desenvolvedor .NET Senior", string company = "Acme") => new()
    {
        ExternalId = "job-1",
        Url = "https://www.linkedin.com/jobs/view/1/",
        Title = title,
        Company = company,
        FullText = fullText,
        RelativeDateText = "1d",
        PublishedAt = publishedAt ?? Now - TimeSpan.FromHours(10)
    };

    private static PostItem CreatePost(string fullText, DateTimeOffset? publishedAt = null) => new()
    {
        ExternalId = "post-1",
        Url = "https://www.linkedin.com/feed/update/urn:li:activity:1/",
        FullText = fullText,
        RelativeDateText = "3h",
        PublishedAt = publishedAt ?? Now - TimeSpan.FromHours(3)
    };

    [Fact]
    public void Classify_StrongDotNetSeniorRemoteFintechWithContact_IsApprovedWithHighScore()
    {
        var job = CreateJob(
            "Vaga para Desenvolvedor .NET Senior, 100% remoto, atuando com C#, ASP.NET Core, SQL Server, Azure, " +
            "microsserviços e EF Core em projetos de pagamentos e PIX para banco digital. " +
            "Envie seu currículo para Fulana Recrutadora: fulana@acme.com.br");

        var result = _classifier.Classify(job);

        result.IsDiscarded.Should().BeFalse();
        result.Score.Should().BeGreaterOrEqualTo(60);
    }

    [Fact]
    public void Classify_NoDotNetMention_IsDiscarded()
    {
        var job = CreateJob("Vaga para desenvolvedor backend sênior remoto, com experiência em Java e Spring Boot.");

        var result = _classifier.Classify(job);

        result.IsDiscarded.Should().BeTrue();
        result.DiscardReason.Should().Contain(".NET");
    }

    [Fact]
    public void Classify_PublishedMoreThanTwoDaysAgo_IsDiscarded()
    {
        var job = CreateJob("Vaga .NET Senior remota", publishedAt: Now - TimeSpan.FromDays(3));

        var result = _classifier.Classify(job);

        result.IsDiscarded.Should().BeTrue();
        result.DiscardReason.Should().Contain("2 dia");
    }

    [Fact]
    public void Classify_PublishedExactlyTwoDaysAgo_IsNotDiscardedByAge()
    {
        var job = CreateJob(
            "Vaga para Desenvolvedor .NET Senior remoto, C#, ASP.NET Core, SQL Server, Azure, microsserviços, pagamentos.",
            publishedAt: Now - TimeSpan.FromDays(2));

        var result = _classifier.Classify(job);

        result.IsDiscarded.Should().BeFalse();
    }

    [Fact]
    public void Classify_PostWithoutJobIndicatorKeywords_IsDiscardedAsNotAJobPost()
    {
        var post = CreatePost("Feliz em compartilhar que fui contratado como Desenvolvedor .NET Senior na Acme!");

        var result = _classifier.Classify(post);

        result.IsDiscarded.Should().BeTrue();
        result.DiscardReason.Should().Contain("não parece ser uma vaga");
    }

    [Fact]
    public void Classify_JuniorLevel_IsPenalizedAndDiscarded()
    {
        var job = CreateJob("Vaga para Desenvolvedor .NET Júnior, remoto, contratando agora.");

        var result = _classifier.Classify(job);

        result.IsDiscarded.Should().BeTrue();
        result.Score.Should().BeLessThan(60);
    }

    [Fact]
    public void Classify_OnSiteRequired_IsPenalizedHeavily()
    {
        var withRemote = CreateJob(
            "Vaga para Desenvolvedor .NET Senior remoto, contratando, C#, ASP.NET Core, SQL Server, Azure, microsserviços, pagamentos, PIX.");
        var onSite = CreateJob(
            "Vaga para Desenvolvedor .NET Senior, contratando, presencial obrigatório, C#, ASP.NET Core, SQL Server, Azure, microsserviços, pagamentos, PIX.");

        var remoteResult = _classifier.Classify(withRemote);
        var onSiteResult = _classifier.Classify(onSite);

        onSiteResult.Score.Should().BeLessThan(remoteResult.Score);
        onSiteResult.IsDiscarded.Should().BeTrue();
    }

    [Fact]
    public void Classify_FrontEndFocusWithoutDotNetBackend_IsPenalized()
    {
        var job = CreateJob("Vaga .NET Senior remoto, contratando, mas o foco principal é front-end com React.");

        var result = _classifier.Classify(job);

        result.Reasons.Should().Contain(r => r.Contains("fora de .NET backend"));
    }

    [Fact]
    public void Classify_HybridInRio_PenalizesLessThanHybridElsewhere()
    {
        var hybridRio = CreateJob("Vaga .NET Senior, contratando, híbrido no Rio de Janeiro, C#, ASP.NET Core, SQL Server, Azure, microsserviços, pagamentos.");
        var hybridElsewhere = CreateJob("Vaga .NET Senior, contratando, híbrido em São Paulo, C#, ASP.NET Core, SQL Server, Azure, microsserviços, pagamentos.");

        var rioResult = _classifier.Classify(hybridRio);
        var elsewhereResult = _classifier.Classify(hybridElsewhere);

        rioResult.Score.Should().BeGreaterThan(elsewhereResult.Score);
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
