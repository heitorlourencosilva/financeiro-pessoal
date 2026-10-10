using FluentAssertions;
using Financeiro.Domain.ValueObjects;
using Xunit;

namespace Financeiro.Tests.Domain;

public class PeriodoTests
{
    [Fact]
    public void Criar_ComDataFimAntesDoInicio_DeveLancarExcecao()
    {
        var inicio = new DateOnly(2026, 10, 10);
        var fim = new DateOnly(2026, 10, 1);

        var act = () => new Periodo(inicio, fim);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Contem_DataDentroDoIntervalo_DeveRetornarTrue()
    {
        var periodo = new Periodo(
            new DateOnly(2026, 10, 1),
            new DateOnly(2026, 10, 31));

        periodo.Contem(new DateOnly(2026, 10, 15)).Should().BeTrue();
    }

    [Fact]
    public void Contem_DataForaDoIntervalo_DeveRetornarFalse()
    {
        var periodo = new Periodo(
            new DateOnly(2026, 10, 1),
            new DateOnly(2026, 10, 31));

        periodo.Contem(new DateOnly(2026, 11, 1)).Should().BeFalse();
    }

    [Fact]
    public void MesCompleto_DeveRetornarPrimeiroEUltimoDia()
    {
        var periodo = Periodo.MesCompleto(2026, 10);

        periodo.Inicio.Should().Be(new DateOnly(2026, 10, 1));
        periodo.Fim.Should().Be(new DateOnly(2026, 10, 31));
    }
}