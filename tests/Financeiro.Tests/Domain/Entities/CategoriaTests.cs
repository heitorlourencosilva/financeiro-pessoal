using FluentAssertions;
using Financeiro.Domain.Entities;
using Xunit;

namespace Financeiro.Tests.Domain.Entities;

public class CategoriaTests
{
    [Fact]
    public void CriarCategoriaRaiz_SemPai_DeveSerValida()
    {
        var categoria = new Categoria("Alimentação", null);

        categoria.Nome.Should().Be("Alimentação");
        categoria.CategoriaPaiId.Should().BeNull();
    }

    [Fact]
    public void CriarSubcategoria_ComPai_DeveSerValida()
    {
        var pai = new Categoria("Alimentação");
        var filha = new Categoria("Restaurante", pai);

        filha.CategoriaPaiId.Should().Be(pai.Id);
    }

    [Fact]
    public void CriarCategoria_ComNomeVazio_DeveLancarExcecao()
    {
        var act = () => new Categoria("", null);

        act.Should().Throw<ArgumentException>();
    }
}