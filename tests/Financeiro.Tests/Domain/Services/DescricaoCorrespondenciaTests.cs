using Financeiro.Domain.Services;
using FluentAssertions;
using Xunit;

namespace Financeiro.Tests.Domain.Services;

public class DescricaoCorrespondenciaTests
{
    [Theory]
    [InlineData("PIX MERCADO CENTRAL", "PIX")]
    [InlineData("PAGAMENTO PIX MERCADO", "PIX")]
    [InlineData("PAGAMENTO VIA PIX", "PIX")]
    public void ContemTermo_ComTermoNoInicioMeioOuFimComLimites_RetornaVerdadeiro(
        string descricao,
        string termo)
    {
        // Arrange

        // Act
        var resultado = DescricaoCorrespondencia.ContemTermo(descricao, termo);

        // Assert
        resultado.Should().BeTrue();
    }

    [Fact]
    public void ContemTermo_ComTermoDentroDeOutraPalavra_RetornaFalso()
    {
        // Arrange
        const string descricao = "SUPERMERCADO";
        const string termo = "MERCADO";

        // Act
        var resultado = DescricaoCorrespondencia.ContemTermo(descricao, termo);

        // Assert
        resultado.Should().BeFalse();
    }

    [Fact]
    public void Similaridade_ComApenasUmTermoEmComum_RetornaZero()
    {
        // Arrange
        const string descricao = "PIX MERCADO";
        const string outraDescricao = "PIX POSTO";

        // Act
        var resultado = DescricaoCorrespondencia.Similaridade(descricao, outraDescricao);

        // Assert
        resultado.Should().Be(0d);
    }

    [Fact]
    public void Similaridade_ComDoisTermosEmComumAcimaDoLimite_RetornaSimilaridade()
    {
        // Arrange
        const string descricao = "PIX MERCADO POSTO";
        const string outraDescricao = "PIX MERCADO FARMACIA";

        // Act
        var resultado = DescricaoCorrespondencia.Similaridade(descricao, outraDescricao);

        // Assert
        resultado.Should().BeApproximately(2d / 3d, 0.0001d);
    }

    [Fact]
    public void Similaridade_ComDescricaoIgualDeUmTermo_RetornaZero()
    {
        // Arrange
        const string descricao = "PIX";

        // Act
        var resultado = DescricaoCorrespondencia.Similaridade(descricao, descricao);

        // Assert
        resultado.Should().Be(0d);
    }

    [Fact]
    public void Similaridade_AbaixoDoLimiteMinimo_RetornaZero()
    {
        // Arrange
        const string descricao = "PIX MERCADO POSTO FARMACIA";
        const string outraDescricao = "PIX MERCADO ACADEMIA ESCOLA";

        // Act
        var resultado = DescricaoCorrespondencia.Similaridade(descricao, outraDescricao);

        // Assert
        resultado.Should().Be(0d);
    }
}
