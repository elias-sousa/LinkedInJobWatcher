using FluentAssertions;
using LinkedInJobWatcher.Core.Text;

namespace LinkedInJobWatcher.Tests.Text;

public class ContentHasherTests
{
    [Fact]
    public void ComputeHash_SameTextDifferentCasingAndSpacing_ProducesSameHash()
    {
        var hash1 = ContentHasher.ComputeHash("Vaga .NET Senior   Remoto!");
        var hash2 = ContentHasher.ComputeHash("vaga .net senior remoto!");

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void ComputeHash_SameTextWithDifferentUrl_ProducesSameHash()
    {
        var hash1 = ContentHasher.ComputeHash("Vaga .NET Senior remoto. Candidate-se em https://empresa.gupy.io/vaga/1");
        var hash2 = ContentHasher.ComputeHash("Vaga .NET Senior remoto. Candidate-se em https://outra-empresa.gupy.io/vaga/2");

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void ComputeHash_DifferentText_ProducesDifferentHash()
    {
        var hash1 = ContentHasher.ComputeHash("Vaga .NET Senior remoto");
        var hash2 = ContentHasher.ComputeHash("Vaga Java Pleno presencial");

        hash1.Should().NotBe(hash2);
    }
}
