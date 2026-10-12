using System.Net;
using System.Net.Http.Json;
using Financeiro.Domain.Entities;
using Financeiro.Domain.Enums;
using Financeiro.Features.Transacoes;
using FluentAssertions;
using Xunit;

namespace Financeiro.Tests.Features.Transacoes;

public class CriarTransacaoTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Criar_TransacaoValida_RetornaSucesso()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var context = await GetDbContextAsync();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);
        await context.SaveChangesAsync();

        var request = new Criar.FeatureRequest(
            Data: new DateOnly(2024, 10, 15),
            Descricao: "Salário",
            Valor: 5000.00m,
            Natureza: NaturezaTransacao.Receita,
            ContaId: conta.Id,
            Contexto: Contexto.Pessoal,
            Pessoa: Pessoa.Eu);

        // Act
        var response = await Client.PostAsJsonAsync("/api/transacoes", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TransacaoResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
        result.Data.Should().Be(new DateOnly(2024, 10, 15));
        result.Descricao.Should().Be("Salário");
        result.Valor.Should().Be(5000.00m);
        result.Moeda.Should().Be("BRL");
        result.Natureza.Should().Be(NaturezaTransacao.Receita);
        result.ContaId.Should().Be(conta.Id);
        result.ContaNome.Should().Be("Nubank");
        result.Contexto.Should().Be(Contexto.Pessoal);
        result.Pessoa.Should().Be(Pessoa.Eu);
        result.Classificada.Should().BeFalse();
        result.ImportadaEm.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Criar_Despesa_RetornaSucesso()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var context = await GetDbContextAsync();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);
        await context.SaveChangesAsync();

        var request = new Criar.FeatureRequest(
            Data: new DateOnly(2024, 10, 15),
            Descricao: "Mercado",
            Valor: 350.50m,
            Natureza: NaturezaTransacao.Despesa,
            ContaId: conta.Id,
            Contexto: Contexto.Familiar,
            Pessoa: Pessoa.Conjuge);

        // Act
        var response = await Client.PostAsJsonAsync("/api/transacoes", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TransacaoResponse>();
        result.Should().NotBeNull();
        result!.Natureza.Should().Be(NaturezaTransacao.Despesa);
        result.Contexto.Should().Be(Contexto.Familiar);
        result.Pessoa.Should().Be(Pessoa.Conjuge);
    }

    [Fact]
    public async Task Criar_Transferencia_RetornaSucesso()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var context = await GetDbContextAsync();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);
        await context.SaveChangesAsync();

        var request = new Criar.FeatureRequest(
            Data: new DateOnly(2024, 10, 15),
            Descricao: "Transferência para poupança",
            Valor: 1000.00m,
            Natureza: NaturezaTransacao.Transferencia,
            ContaId: conta.Id);

        // Act
        var response = await Client.PostAsJsonAsync("/api/transacoes", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TransacaoResponse>();
        result.Should().NotBeNull();
        result!.Natureza.Should().Be(NaturezaTransacao.Transferencia);
    }

    [Fact]
    public async Task Criar_Investimento_RetornaSucesso()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var context = await GetDbContextAsync();
        var conta = new Conta("XP", TipoConta.Investimento, null);
        context.Contas.Add(conta);
        await context.SaveChangesAsync();

        var request = new Criar.FeatureRequest(
            Data: new DateOnly(2024, 10, 15),
            Descricao: "Aporte Tesouro Direto",
            Valor: 5000.00m,
            Natureza: NaturezaTransacao.Investimento,
            ContaId: conta.Id,
            Contexto: Contexto.Investimentos);

        // Act
        var response = await Client.PostAsJsonAsync("/api/transacoes", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TransacaoResponse>();
        result.Should().NotBeNull();
        result!.Natureza.Should().Be(NaturezaTransacao.Investimento);
        result.Contexto.Should().Be(Contexto.Investimentos);
    }

    [Fact]
    public async Task Criar_ContaInexistente_RetornaNotFound()
    {
        // Arrange
        await ClearDatabaseAsync();
        var contaIdInexistente = Guid.NewGuid();

        var request = new Criar.FeatureRequest(
            Data: new DateOnly(2024, 10, 15),
            Descricao: "Teste",
            Valor: 100.00m,
            Natureza: NaturezaTransacao.Receita,
            ContaId: contaIdInexistente);

        // Act
        var response = await Client.PostAsJsonAsync("/api/transacoes", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Criar_DataVazia_RetornaErroValidacao()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var context = await GetDbContextAsync();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);
        await context.SaveChangesAsync();

        var request = new Criar.FeatureRequest(
            Data: default,
            Descricao: "Teste",
            Valor: 100.00m,
            Natureza: NaturezaTransacao.Receita,
            ContaId: conta.Id);

        // Act
        var response = await Client.PostAsJsonAsync("/api/transacoes", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Criar_DescricaoVazia_RetornaErroValidacao()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var context = await GetDbContextAsync();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);
        await context.SaveChangesAsync();

        var request = new Criar.FeatureRequest(
            Data: new DateOnly(2024, 10, 15),
            Descricao: "",
            Valor: 100.00m,
            Natureza: NaturezaTransacao.Receita,
            ContaId: conta.Id);

        // Act
        var response = await Client.PostAsJsonAsync("/api/transacoes", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Criar_DescricaoMuitoLonga_RetornaErroValidacao()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var context = await GetDbContextAsync();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);
        await context.SaveChangesAsync();

        var request = new Criar.FeatureRequest(
            Data: new DateOnly(2024, 10, 15),
            Descricao: new string('A', 201),
            Valor: 100.00m,
            Natureza: NaturezaTransacao.Receita,
            ContaId: conta.Id);

        // Act
        var response = await Client.PostAsJsonAsync("/api/transacoes", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Criar_ValorZero_RetornaErroValidacao()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var context = await GetDbContextAsync();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);
        await context.SaveChangesAsync();

        var request = new Criar.FeatureRequest(
            Data: new DateOnly(2024, 10, 15),
            Descricao: "Teste",
            Valor: 0m,
            Natureza: NaturezaTransacao.Receita,
            ContaId: conta.Id);

        // Act
        var response = await Client.PostAsJsonAsync("/api/transacoes", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Criar_ValorNegativo_RetornaErroValidacao()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var context = await GetDbContextAsync();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);
        await context.SaveChangesAsync();

        var request = new Criar.FeatureRequest(
            Data: new DateOnly(2024, 10, 15),
            Descricao: "Teste",
            Valor: -100.00m,
            Natureza: NaturezaTransacao.Receita,
            ContaId: conta.Id);

        // Act
        var response = await Client.PostAsJsonAsync("/api/transacoes", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
