using FluentAssertions;
using Financeiro.Domain.Enums;
using Xunit;

namespace Financeiro.Tests.Domain;

public class EnumsTests
{
    [Fact]
    public void NaturezaTransacao_DeveTerValoresEsperados()
    {
        Enum.GetValues<NaturezaTransacao>().Should().BeEquivalentTo(new[]
        {
            NaturezaTransacao.Receita,
            NaturezaTransacao.Despesa,
            NaturezaTransacao.Transferencia,
            NaturezaTransacao.Investimento
        });
    }

    [Fact]
    public void Contexto_DeveTerValoresEsperados()
    {
        Enum.GetValues<Contexto>().Should().BeEquivalentTo(new[]
        {
            Contexto.Pessoal,
            Contexto.Familiar,
            Contexto.YouTube,
            Contexto.Blog,
            Contexto.Site,
            Contexto.Investimentos,
            Contexto.Outro
        });
    }

    [Fact]
    public void Pessoa_DeveTerValoresEsperados()
    {
        Enum.GetValues<Pessoa>().Should().BeEquivalentTo(new[]
        {
            Pessoa.Eu,
            Pessoa.Conjuge,
            Pessoa.Filho,
            Pessoa.Outro
        });
    }
}