using Financeiro.Domain.Entities;
using Financeiro.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Financeiro.Tests.Domain.Entities;

public class RegraClassificacaoAutomaticaTests
{
    [Fact]
    public void CriarRegra_ComDescricaoValidaNormalizaDescricao()
    {
        // Arrange
        var categoria = new Categoria("Alimentação");

        // Act
        var regra = new RegraClassificacaoAutomatica(
            "  pix   enviado 123456",
            categoria,
            Contexto.Pessoal,
            Pessoa.Eu,
            prioridade: 1);

        // Assert
        regra.DescricaoCorrespondenciaNormalizada.Should().Be("PIX ENVIADO");
        regra.CategoriaId.Should().Be(categoria.Id);
        regra.Ativa.Should().BeTrue();
    }

    [Fact]
    public void CriarRegra_ComDescricaoQueNormalizaParaVazio_LancaArgumentException()
    {
        // Arrange
        var categoria = new Categoria("Alimentação");

        // Act
        var act = () => new RegraClassificacaoAutomatica(
            "!!!",
            categoria,
            Contexto.Pessoal,
            Pessoa.Eu,
            prioridade: 1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*ao menos uma letra ou número*");
    }
}
