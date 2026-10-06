using FluentAssertions;
using LinkedInJobWatcher.Core.Models;
using LinkedInJobWatcher.Core.Parsing;

namespace LinkedInJobWatcher.Tests.Parsing;

public class ContactExtractorTests
{
    private readonly ContactExtractor _extractor = new();

    [Fact]
    public void Extract_PlainEmail_IsFound()
    {
        var result = _extractor.Extract("Envie seu currículo para contato@acmetecnologia.com.br", null, null);

        result.Email.Should().Be("contato@acmetecnologia.com.br");
    }

    [Theory]
    [InlineData("Envie para fulana [at] acmetecnologia.com.br", "fulana@acmetecnologia.com.br")]
    [InlineData("Envie para fulana(arroba)acmetecnologia.com.br", "fulana@acmetecnologia.com.br")]
    [InlineData("Envie para fulana (at) acmetecnologia.com.br", "fulana@acmetecnologia.com.br")]
    public void Extract_ObfuscatedEmail_IsDeobfuscatedAndFound(string text, string expectedEmail)
    {
        var result = _extractor.Extract(text, null, null);

        result.Email.Should().Be(expectedEmail);
    }

    [Fact]
    public void Extract_ExampleDomainEmail_IsIgnored()
    {
        var result = _extractor.Extract("Modelo: nome@exemplo.com. Contato real: real@acmetecnologia.com.br", null, null);

        result.Email.Should().Be("real@acmetecnologia.com.br");
    }

    [Theory]
    [InlineData("Candidate-se em https://acmetecnologia.gupy.io/vagas/dotnet-senior", "Gupy")]
    [InlineData("Inscrições: https://empresa.kenoby.com/vaga/123", "Kenoby")]
    [InlineData("Formulário: https://forms.gle/abc123", "Google Forms")]
    [InlineData("Link: https://lnkd.in/abc123", "Link encurtado")]
    public void Extract_ApplicationLink_IdentifiesPlatform(string text, string expectedPlatform)
    {
        var result = _extractor.Extract(text, null, null);

        result.ApplicationPlatform.Should().Be(expectedPlatform);
    }

    [Fact]
    public void Extract_ContactNameMentionInText_IsFound()
    {
        var result = _extractor.Extract("Envie seu currículo para Fulana Recrutadora até sexta-feira.", null, null);

        result.ContactName.Should().Be("Fulana Recrutadora");
    }

    [Fact]
    public void Extract_RecruiterAuthorHeadline_UsesAuthorAsContact()
    {
        var result = _extractor.Extract("Vaga aberta, sem detalhes de contato no texto.", "Fulana Recrutadora", "Tech Recruiter na Acme");

        result.ContactName.Should().Be("Fulana Recrutadora");
    }

    [Fact]
    public void Extract_NonRecruiterAuthorHeadline_DoesNotUseAuthorAsContact()
    {
        var result = _extractor.Extract("Vaga aberta, sem detalhes de contato no texto.", "João Dev", "Desenvolvedor .NET na Acme");

        result.ContactName.Should().BeNull();
    }

    [Fact]
    public void Extract_SubjectHint_IsFound()
    {
        var result = _extractor.Extract("Candidate-se e coloque no assunto: VAGA-DOTNET-SR-003.", null, null);

        result.SubjectHint.Should().Be("VAGA-DOTNET-SR-003");
    }

    [Theory]
    [InlineData("Modelo de contratação: CLT", EmploymentType.Clt)]
    [InlineData("Contrato via PJ", EmploymentType.Pj)]
    [InlineData("Trabalho como cooperado", EmploymentType.Cooperado)]
    [InlineData("Sem informação de contratação", EmploymentType.Unknown)]
    public void Extract_EmploymentType_IsDetected(string text, EmploymentType expected)
    {
        var result = _extractor.Extract(text, null, null);

        result.EmploymentType.Should().Be(expected);
    }

    [Fact]
    public void Extract_SalaryRange_IsFound()
    {
        var result = _extractor.Extract("Faixa salarial: R$ 12.000 a R$ 16.000", null, null);

        result.SalaryRange.Should().Contain("12.000");
    }
}
