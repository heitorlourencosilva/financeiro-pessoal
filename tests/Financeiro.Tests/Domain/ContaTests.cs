using FluentAssertions;
using Financeiro.Domain.Entities;
using Financeiro.Domain.Enums;
using Xunit;

namespace Financeiro.Tests.Domain;

public class ContaTests
{
    [Fact]
    public void CriarConta_DeveTerContextoNulo()
    {
        var conta = new Conta("Nubank", TipoConta.ContaCorrente);

        conta.Nome.Should().Be("Nubank");
        // Conta NÃO tem contexto — D-002
        conta.GetType().GetProperty("Contexto").Should().BeNull();
    }

    [Fact]
    public void CriarConta_ComNomeVazio_DeveLancarExcecao()
    {
        var act = () => new Conta("", TipoConta.ContaCorrente);

        act.Should().Throw<ArgumentException>();
    }
}