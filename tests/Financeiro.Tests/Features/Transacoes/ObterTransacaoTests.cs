using System.Net;
using System.Net.Http.Json;
using Financeiro.Domain.Entities;
using Financeiro.Domain.Enums;
using Financeiro.Domain.ValueObjects;
using Financeiro.Features.Transacoes;
using FluentAssertions;
using Xunit;

namespace Financeiro.Tests.Features.Transacoes;

public class ObterTransacaoTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Obter_TransacaoExistente_RetornaTransacao()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var context = await GetDbContextAsync();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, "Nu Pagamentos");
        context.Contas.Add(conta);

        var transacao = new Transacao(
            new DateOnly(2024, 10, 15),
            "Salário",
            new Dinheiro(5000m, "BRL"),
            NaturezaTransacao.Receita,
            conta,
            Contexto.Pessoal,
            Pessoa.Eu);

        context.Transacoes.Add(transacao);
        await context.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/transacoes/{transacao.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TransacaoResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(transacao.Id);
        result.Data.Should().Be(new DateOnly(2024, 10, 15));
        result.Descricao.Should().Be("Salário");
        result.Valor.Should().Be(5000m);
        result.Moeda.Should().Be("BRL");
        result.Natureza.Should().Be(NaturezaTransacao.Receita);
        result.ContaId.Should().Be(conta.Id);
        result.ContaNome.Should().Be("Nubank");
        result.Contexto.Should().Be(Contexto.Pessoal);
        result.Pessoa.Should().Be(Pessoa.Eu);
        result.Classificada.Should().BeFalse();
    }

    [Fact]
    public async Task Obter_TransacaoInexistente_RetornaNotFound()
    {
        // Arrange
        await ClearDatabaseAsync();
        var idInexistente = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/transacoes/{idInexistente}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Obter_TransacaoComCategoria_RetornaCategoria()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var context = await GetDbContextAsync();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        var categoria = new Categoria("Alimentação", null);
        context.Contas.Add(conta);
        context.Categorias.Add(categoria);

        var transacao = new Transacao(
            new DateOnly(2024, 10, 15),
            "Mercado",
            new Dinheiro(300m, "BRL"),
            NaturezaTransacao.Despesa,
            conta,
            Contexto.Familiar,
            Pessoa.Conjuge);

        transacao.Classificar(categoria, Contexto.Familiar, Pessoa.Conjuge, null);
        context.Transacoes.Add(transacao);
        await context.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/transacoes/{transacao.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TransacaoResponse>();
        result.Should().NotBeNull();
        result!.CategoriaId.Should().Be(categoria.Id);
        result.CategoriaNome.Should().Be("Alimentação");
        result.Classificada.Should().BeTrue();
        result.ClassificadaEm.Should().NotBeNull();
    }

    [Fact]
    public async Task Obter_TransacaoSemCategoria_RetornaCategoriaNull()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var context = await GetDbContextAsync();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);

        var transacao = new Transacao(
            new DateOnly(2024, 10, 15),
            "Desconhecido",
            new Dinheiro(100m, "BRL"),
            NaturezaTransacao.Despesa,
            conta,
            Contexto.Pessoal,
            Pessoa.Eu);

        context.Transacoes.Add(transacao);
        await context.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/transacoes/{transacao.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TransacaoResponse>();
        result.Should().NotBeNull();
        result!.CategoriaId.Should().BeNull();
        result.CategoriaNome.Should().BeNull();
        result.Classificada.Should().BeFalse();
        result.ClassificadaEm.Should().BeNull();
    }
}
