using System.Net;
using System.Net.Http.Json;
using Financeiro.Domain.Entities;
using Financeiro.Domain.Enums;
using Financeiro.Domain.ValueObjects;
using Financeiro.Features.Transacoes;
using FluentAssertions;
using Xunit;

namespace Financeiro.Tests.Features.Transacoes;

public class ListarTransacoesTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Listar_SemTransacoes_RetornaArrayVazio()
    {
        // Arrange
        await ClearDatabaseAsync();

        // Act
        var response = await Client.GetAsync("/api/transacoes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TransacaoResponse[]>();
        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }

    [Fact]
    public async Task Listar_ComTransacoes_RetornaTodasOrdenadasPorDataDecrescente()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var context = await GetDbContextAsync();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);

        var t1 = new Transacao(new DateOnly(2024, 10, 10), "Mercado", new Dinheiro(100m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu);
        var t2 = new Transacao(new DateOnly(2024, 10, 15), "Salário", new Dinheiro(5000m, "BRL"), NaturezaTransacao.Receita, conta, Contexto.Pessoal, Pessoa.Eu);
        var t3 = new Transacao(new DateOnly(2024, 10, 05), "Farmácia", new Dinheiro(50m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu);

        context.Transacoes.AddRange(t1, t2, t3);
        await context.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/transacoes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TransacaoResponse[]>();
        result.Should().NotBeNull();
        result!.Should().HaveCount(3);
        result![0].Data.Should().Be(new DateOnly(2024, 10, 15)); // Mais recente
        result[1].Data.Should().Be(new DateOnly(2024, 10, 10));
        result[2].Data.Should().Be(new DateOnly(2024, 10, 05)); // Mais antiga
    }

    [Fact]
    public async Task Listar_TransacoesDeVariasNaturezas_RetornaTodas()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var context = await GetDbContextAsync();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);

        context.Transacoes.AddRange(
            new Transacao(new DateOnly(2024, 10, 10), "Salário", new Dinheiro(5000m, "BRL"), NaturezaTransacao.Receita, conta, Contexto.Pessoal, Pessoa.Eu),
            new Transacao(new DateOnly(2024, 10, 11), "Mercado", new Dinheiro(300m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Familiar, Pessoa.Conjuge),
            new Transacao(new DateOnly(2024, 10, 12), "Transferência", new Dinheiro(1000m, "BRL"), NaturezaTransacao.Transferencia, conta, Contexto.Pessoal, Pessoa.Eu),
            new Transacao(new DateOnly(2024, 10, 13), "Aporte", new Dinheiro(2000m, "BRL"), NaturezaTransacao.Investimento, conta, Contexto.Investimentos, Pessoa.Eu)
        );
        await context.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/transacoes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TransacaoResponse[]>();
        result.Should().NotBeNull();
        result!.Should().HaveCount(4);
        result!.Select(t => t.Natureza).Should().Contain(new[]
        {
            NaturezaTransacao.Receita,
            NaturezaTransacao.Despesa,
            NaturezaTransacao.Transferencia,
            NaturezaTransacao.Investimento
        });
    }

    [Fact]
    public async Task Listar_TransacoesDeVariosContextos_RetornaTodas()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var context = await GetDbContextAsync();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);

        context.Transacoes.AddRange(
            new Transacao(new DateOnly(2024, 10, 10), "Pessoal", new Dinheiro(100m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu),
            new Transacao(new DateOnly(2024, 10, 11), "Familiar", new Dinheiro(200m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Familiar, Pessoa.Conjuge),
            new Transacao(new DateOnly(2024, 10, 12), "YouTube", new Dinheiro(300m, "BRL"), NaturezaTransacao.Receita, conta, Contexto.YouTube, Pessoa.Eu),
            new Transacao(new DateOnly(2024, 10, 13), "Blog", new Dinheiro(400m, "BRL"), NaturezaTransacao.Receita, conta, Contexto.Blog, Pessoa.Eu)
        );
        await context.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/transacoes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TransacaoResponse[]>();
        result.Should().NotBeNull();
        result!.Should().HaveCount(4);
        result!.Select(t => t.Contexto).Should().Contain(new[]
        {
            Contexto.Pessoal,
            Contexto.Familiar,
            Contexto.YouTube,
            Contexto.Blog
        });
    }

    [Fact]
    public async Task Listar_TransacoesDeVariasPessoas_RetornaTodas()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var context = await GetDbContextAsync();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);

        context.Transacoes.AddRange(
            new Transacao(new DateOnly(2024, 10, 10), "Eu", new Dinheiro(100m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu),
            new Transacao(new DateOnly(2024, 10, 11), "Cônjuge", new Dinheiro(200m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Familiar, Pessoa.Conjuge),
            new Transacao(new DateOnly(2024, 10, 12), "Filho", new Dinheiro(300m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Familiar, Pessoa.Filho),
            new Transacao(new DateOnly(2024, 10, 13), "Outro", new Dinheiro(400m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Outro)
        );
        await context.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/transacoes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TransacaoResponse[]>();
        result.Should().NotBeNull();
        result!.Should().HaveCount(4);
        result!.Select(t => t.Pessoa).Should().Contain(new[]
        {
            Pessoa.Eu,
            Pessoa.Conjuge,
            Pessoa.Filho,
            Pessoa.Outro
        });
    }
}
