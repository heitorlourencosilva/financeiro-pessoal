using FluentAssertions;
using Financeiro.Domain.Entities;
using Xunit;

namespace Financeiro.Tests.Domain.Entities;

public class TagTests
{
    [Fact]
    public void CriarTag_ComNome_DeveSerValida()
    {
        var tag = new Tag("viagem");

        tag.Nome.Should().Be("viagem");
    }

    [Fact]
    public void CriarTag_ComNomeVazio_DeveLancarExcecao()
    {
        var act = () => new Tag("");

        act.Should().Throw<ArgumentException>();
    }
}