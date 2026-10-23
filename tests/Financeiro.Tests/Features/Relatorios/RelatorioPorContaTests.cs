using System.Net;
using System.Net.Http.Json;
using Financeiro.Domain.Entities;
using Financeiro.Domain.Enums;
using Financeiro.Domain.ValueObjects;
using Financeiro.Features.Relatorios.PorConta;
using Financeiro.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Financeiro.Tests.Features.Relatorios;

public class RelatorioPorContaTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Relatorio_SemTransacoes_RetornaContasComZerosETotalZerado()
    {
        await ClearDatabaseAsync();
        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        context.Contas.Add(new Conta("Nubank", TipoConta.ContaCorrente, null));
        await context.SaveChangesAsync();

        var response = await Client.GetAsync("/api/relatorios/por-conta");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RelatorioPorContaResponse>();
        result!.Linhas.Should().HaveCount(1);
        result!.Linhas[0].TotalReceitas.Should().Be(0m);
        result!.Linhas[0].SaldoPeriodo.Should().Be(0m);
        result!.Total.SaldoPeriodo.Should().Be(0m);
    }

    [Fact]
    public async Task Relatorio_ComReceitasEDespesas_CalculaSaldoPorConta()
    {
        await ClearDatabaseAsync();
        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var nubank = new Conta("Nubank", TipoConta.ContaCorrente, null);
        var itau = new Conta("Itaú", TipoConta.ContaCorrente, null);
        context.Contas.AddRange(nubank, itau);
        context.Transacoes.AddRange(
            new Transacao(new DateOnly(2024, 10, 10), "Salário", new Dinheiro(5000m, "BRL"), NaturezaTransacao.Receita, nubank, Contexto.Pessoal, Pessoa.Eu),
            new Transacao(new DateOnly(2024, 10, 11), "Mercado", new Dinheiro(300m, "BRL"), NaturezaTransacao.Despesa, nubank, Contexto.Pessoal, Pessoa.Eu),
            new Transacao(new DateOnly(2024, 10, 12), "Aluguel", new Dinheiro(1500m, "BRL"), NaturezaTransacao.Despesa, itau, Contexto.Familiar, Pessoa.Eu));
        await context.SaveChangesAsync();

        var response = await Client.GetAsync("/api/relatorios/por-conta");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RelatorioPorContaResponse>();

        var linhaNubank = result!.Linhas.Single(l => l.ContaNome == "Nubank");
        linhaNubank.TotalReceitas.Should().Be(5000m);
        linhaNubank.TotalDespesas.Should().Be(300m);
        linhaNubank.SaldoPeriodo.Should().Be(4700m);

        var linhaItau = result!.Linhas.Single(l => l.ContaNome == "Itaú");
        linhaItau.TotalReceitas.Should().Be(0m);
        linhaItau.TotalDespesas.Should().Be(1500m);
        linhaItau.SaldoPeriodo.Should().Be(-1500m);

        result!.Total.TotalReceitas.Should().Be(5000m);
        result!.Total.TotalDespesas.Should().Be(1800m);
        result!.Total.SaldoPeriodo.Should().Be(3200m);
    }

    [Fact]
    public async Task Relatorio_ComTransferenciasEInvestimentos_NaoAfetaSaldo()
    {
        await ClearDatabaseAsync();
        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);
        context.Transacoes.AddRange(
            new Transacao(new DateOnly(2024, 10, 10), "Salário", new Dinheiro(5000m, "BRL"), NaturezaTransacao.Receita, conta, Contexto.Pessoal, Pessoa.Eu),
            new Transacao(new DateOnly(2024, 10, 11), "Transf", new Dinheiro(1000m, "BRL"), NaturezaTransacao.Transferencia, conta, Contexto.Pessoal, Pessoa.Eu),
            new Transacao(new DateOnly(2024, 10, 12), "Aporte", new Dinheiro(2000m, "BRL"), NaturezaTransacao.Investimento, conta, Contexto.Investimentos, Pessoa.Eu));
        await context.SaveChangesAsync();

        var response = await Client.GetAsync("/api/relatorios/por-conta");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RelatorioPorContaResponse>();
        var linha = result!.Linhas.Single();

        linha.TotalReceitas.Should().Be(5000m);
        linha.TotalDespesas.Should().Be(0m);
        linha.TotalTransferencias.Should().Be(1000m);
        linha.TotalInvestimentos.Should().Be(2000m);
        linha.SaldoPeriodo.Should().Be(5000m); // D-001 / D-003: neutros fora
    }

    [Fact]
    public async Task Relatorio_ComPeriodo_FiltraTransacoes()
    {
        await ClearDatabaseAsync();
        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);
        context.Transacoes.AddRange(
            new Transacao(new DateOnly(2024, 10, 01), "Fora", new Dinheiro(100m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu),
            new Transacao(new DateOnly(2024, 10, 15), "Dentro", new Dinheiro(200m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu),
            new Transacao(new DateOnly(2024, 10, 31), "Fora", new Dinheiro(400m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu));
        await context.SaveChangesAsync();

        var response = await Client.GetAsync("/api/relatorios/por-conta?dataInicio=2024-10-10&dataFim=2024-10-20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RelatorioPorContaResponse>();
        result!.Linhas.Single().TotalDespesas.Should().Be(200m);
    }

    [Fact]
    public async Task Relatorio_ContasOrdenadasPorNome_ComTotalNoFinal()
    {
        await ClearDatabaseAsync();
        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        context.Contas.AddRange(
            new Conta("Zebra", TipoConta.ContaCorrente, null),
            new Conta("Alpha", TipoConta.ContaCorrente, null),
            new Conta("Mike", TipoConta.ContaCorrente, null));
        await context.SaveChangesAsync();

        var response = await Client.GetAsync("/api/relatorios/por-conta");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RelatorioPorContaResponse>();
        result!.Linhas.Select(l => l.ContaNome)
            .Should().BeInAscendingOrder()
            .And.Equal("Alpha", "Mike", "Zebra");
        result!.Total.ContaNome.Should().Be("TOTAL");
    }
}