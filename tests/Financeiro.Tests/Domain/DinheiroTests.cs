using FluentAssertions;
using Financeiro.Domain.ValueObjects;
using Xunit;

namespace Financeiro.Tests.Domain;

public class DinheiroTests
{
    [Fact]
    public void Criar_ComValorPositivo_DeveArmazenarValorEMoeda()
    {
        var dinheiro = new Dinheiro(1500.50m, "BRL");

        dinheiro.Valor.Should().Be(1500.50m);
        dinheiro.Moeda.Should().Be("BRL");
    }

    [Fact]
    public void Criar_ComMoedaVazia_DeveLancarExcecao()
    {
        var act = () => new Dinheiro(100m, "");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Somar_MesmaMoeda_DeveRetornarSomaCorreta()
    {
        var a = new Dinheiro(100m, "BRL");
        var b = new Dinheiro(50m, "BRL");

        var resultado = a + b;

        resultado.Valor.Should().Be(150m);
        resultado.Moeda.Should().Be("BRL");
    }

    [Fact]
    public void Somar_MoedasDiferentes_DeveLancarExcecao()
    {
        var a = new Dinheiro(100m, "BRL");
        var b = new Dinheiro(50m, "USD");

        var act = () => a + b;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Igualdade_DeveSerPorValorEMoeda()
    {
        var a = new Dinheiro(100m, "BRL");
        var b = new Dinheiro(100m, "BRL");

        a.Should().Be(b);
    }
}