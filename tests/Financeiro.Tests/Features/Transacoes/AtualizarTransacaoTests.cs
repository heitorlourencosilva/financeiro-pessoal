using System.Net;
using System.Net.Http.Json;
using Financeiro.Domain.Entities;
using Financeiro.Domain.Enums;
using Financeiro.Domain.ValueObjects;
using Financeiro.Features.Transacoes;
using Financeiro.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Financeiro.Tests.Features.Transacoes;

public class AtualizarTransacaoTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Atualizar_TransacaoExistente_RetornaSucesso()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
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

        var request = new Atualizar.FeatureRequest(
            Data: new DateOnly(2024, 10, 20),
            Descricao: "Salário Atualizado",
            Valor: 5500m,
            Natureza: NaturezaTransacao.Receita,
            ContaId: conta.Id,
            Contexto: Contexto.Familiar,
            Pessoa: Pessoa.Conjuge,
            Observacao: "Corrigido");

        // Act
        var response = await Client.PutAsJsonAsync($"/api/transacoes/{transacao.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TransacaoResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(transacao.Id);
        result.Data.Should().Be(new DateOnly(2024, 10, 20));
        result.Descricao.Should().Be("Salário Atualizado");
        result.Valor.Should().Be(5500m);
        result.Contexto.Should().Be(Contexto.Familiar);
        result.Pessoa.Should().Be(Pessoa.Conjuge);
        result.Observacao.Should().Be("Corrigido");
    }

    [Fact]
    public async Task Atualizar_TransacaoInexistente_RetornaNotFound()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);
        await context.SaveChangesAsync();

        var idInexistente = Guid.NewGuid();
        var request = new Atualizar.FeatureRequest(
            Data: new DateOnly(2024, 10, 20),
            Descricao: "Teste",
            Valor: 100m,
            Natureza: NaturezaTransacao.Receita,
            ContaId: conta.Id,
            Contexto: Contexto.Pessoal,
            Pessoa: Pessoa.Eu,
            Observacao: null);

        // Act
        var response = await Client.PutAsJsonAsync($"/api/transacoes/{idInexistente}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Atualizar_ContaInexistente_RetornaNotFound()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
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

        var contaIdInexistente = Guid.NewGuid();
        var request = new Atualizar.FeatureRequest(
            Data: new DateOnly(2024, 10, 20),
            Descricao: "Teste",
            Valor: 100m,
            Natureza: NaturezaTransacao.Receita,
            ContaId: contaIdInexistente,
            Contexto: Contexto.Pessoal,
            Pessoa: Pessoa.Eu,
            Observacao: null);

        // Act
        var response = await Client.PutAsJsonAsync($"/api/transacoes/{transacao.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Atualizar_MudarConta_RetornaSucesso()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta1 = new Conta("Nubank", TipoConta.ContaCorrente, null);
        var conta2 = new Conta("Inter", TipoConta.ContaCorrente, null);
        context.Contas.AddRange(conta1, conta2);

        var transacao = new Transacao(
            new DateOnly(2024, 10, 15),
            "Compra",
            new Dinheiro(100m, "BRL"),
            NaturezaTransacao.Despesa,
            conta1,
            Contexto.Pessoal,
            Pessoa.Eu);

        context.Transacoes.Add(transacao);
        await context.SaveChangesAsync();

        var request = new Atualizar.FeatureRequest(
            Data: transacao.Data,
            Descricao: transacao.DescricaoOriginal,
            Valor: transacao.Valor.Valor,
            Natureza: transacao.Natureza,
            ContaId: conta2.Id,
            Contexto: transacao.Contexto,
            Pessoa: transacao.Pessoa,
            Observacao: "Mudou de conta");

        // Act
        var response = await Client.PutAsJsonAsync($"/api/transacoes/{transacao.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TransacaoResponse>();
        result.Should().NotBeNull();
        result!.ContaId.Should().Be(conta2.Id);
        result.ContaNome.Should().Be("Inter");
    }

    [Fact]
    public async Task Atualizar_DataVazia_RetornaErroValidacao()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);

        var transacao = new Transacao(
            new DateOnly(2024, 10, 15),
            "Teste",
            new Dinheiro(100m, "BRL"),
            NaturezaTransacao.Receita,
            conta,
            Contexto.Pessoal,
            Pessoa.Eu);

        context.Transacoes.Add(transacao);
        await context.SaveChangesAsync();

        var request = new Atualizar.FeatureRequest(
            Data: default,
            Descricao: "Teste",
            Valor: 100m,
            Natureza: NaturezaTransacao.Receita,
            ContaId: conta.Id,
            Contexto: Contexto.Pessoal,
            Pessoa: Pessoa.Eu,
            Observacao: null);

        // Act
        var response = await Client.PutAsJsonAsync($"/api/transacoes/{transacao.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Atualizar_DescricaoVazia_RetornaErroValidacao()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);

        var transacao = new Transacao(
            new DateOnly(2024, 10, 15),
            "Teste",
            new Dinheiro(100m, "BRL"),
            NaturezaTransacao.Receita,
            conta,
            Contexto.Pessoal,
            Pessoa.Eu);

        context.Transacoes.Add(transacao);
        await context.SaveChangesAsync();

        var request = new Atualizar.FeatureRequest(
            Data: new DateOnly(2024, 10, 15),
            Descricao: "",
            Valor: 100m,
            Natureza: NaturezaTransacao.Receita,
            ContaId: conta.Id,
            Contexto: Contexto.Pessoal,
            Pessoa: Pessoa.Eu,
            Observacao: null);

        // Act
        var response = await Client.PutAsJsonAsync($"/api/transacoes/{transacao.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Atualizar_ValorZero_RetornaErroValidacao()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);

        var transacao = new Transacao(
            new DateOnly(2024, 10, 15),
            "Teste",
            new Dinheiro(100m, "BRL"),
            NaturezaTransacao.Receita,
            conta,
            Contexto.Pessoal,
            Pessoa.Eu);

        context.Transacoes.Add(transacao);
        await context.SaveChangesAsync();

        var request = new Atualizar.FeatureRequest(
            Data: new DateOnly(2024, 10, 15),
            Descricao: "Teste",
            Valor: 0m,
            Natureza: NaturezaTransacao.Receita,
            ContaId: conta.Id,
            Contexto: Contexto.Pessoal,
            Pessoa: Pessoa.Eu,
            Observacao: null);

        // Act
        var response = await Client.PutAsJsonAsync($"/api/transacoes/{transacao.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
