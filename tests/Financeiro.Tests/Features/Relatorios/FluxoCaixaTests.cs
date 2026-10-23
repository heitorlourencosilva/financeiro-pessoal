using System.Net;
using System.Net.Http.Json;
using Financeiro.Domain.Entities;
using Financeiro.Domain.Enums;
using Financeiro.Domain.ValueObjects;
using Financeiro.Features.Relatorios.FluxoCaixa;
using Financeiro.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Financeiro.Tests.Features.Relatorios;

public class FluxoCaixaTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task FluxoCaixa_SemPeriodo_RetornaBadRequest()
    {
        await ClearDatabaseAsync();

        var response = await Client.GetAsync("/api/relatorios/fluxo-caixa");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task FluxoCaixa_DataInicioMaiorQueDataFim_RetornaBadRequest()
    {
        await ClearDatabaseAsync();

        var response = await Client.GetAsync("/api/relatorios/fluxo-caixa?dataInicio=2024-10-20&dataFim=2024-10-10");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task FluxoCaixa_SemTransacoes_RetornaTudoZerado()
    {
        await ClearDatabaseAsync();

        var response = await Client.GetAsync("/api/relatorios/fluxo-caixa?dataInicio=2024-10-01&dataFim=2024-10-31");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<FluxoCaixaResponse>();
        result!.SaldoInicial.Should().Be(0m);
        result!.TotalEntradas.Should().Be(0m);
        result!.TotalSaidas.Should().Be(0m);
        result!.SaldoFinal.Should().Be(0m);
        result!.Diario.Should().BeEmpty();
    }

    [Fact]
    public async Task FluxoCaixa_ComSaldoAnterior_CalculaSaldoInicial()
    {
        await ClearDatabaseAsync();
        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);
        context.Transacoes.AddRange(
            new Transacao(new DateOnly(2024, 09, 01), "Salário Set", new Dinheiro(5000m, "BRL"), NaturezaTransacao.Receita, conta, Contexto.Pessoal, Pessoa.Eu),
            new Transacao(new DateOnly(2024, 09, 15), "Mercado Set", new Dinheiro(1000m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu));
        await context.SaveChangesAsync();

        var response = await Client.GetAsync("/api/relatorios/fluxo-caixa?dataInicio=2024-10-01&dataFim=2024-10-31");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<FluxoCaixaResponse>();
        result!.SaldoInicial.Should().Be(4000m); // 5000 - 1000
        result!.SaldoFinal.Should().Be(4000m);
        result!.Diario.Should().BeEmpty();
    }

    [Fact]
    public async Task FluxoCaixa_ComEntradasESaidas_CalculaTotaisEDiario()
    {
        await ClearDatabaseAsync();
        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);
        context.Transacoes.AddRange(
            new Transacao(new DateOnly(2024, 10, 05), "Salário", new Dinheiro(5000m, "BRL"), NaturezaTransacao.Receita, conta, Contexto.Pessoal, Pessoa.Eu),
            new Transacao(new DateOnly(2024, 10, 10), "Mercado", new Dinheiro(300m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu),
            new Transacao(new DateOnly(2024, 10, 10), "Farmácia", new Dinheiro(200m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu),
            new Transacao(new DateOnly(2024, 10, 20), "Freela", new Dinheiro(1000m, "BRL"), NaturezaTransacao.Receita, conta, Contexto.YouTube, Pessoa.Eu));
        await context.SaveChangesAsync();

        var response = await Client.GetAsync("/api/relatorios/fluxo-caixa?dataInicio=2024-10-01&dataFim=2024-10-31");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<FluxoCaixaResponse>();
        result!.TotalEntradas.Should().Be(6000m);
        result!.TotalSaidas.Should().Be(500m);
        result!.SaldoFinal.Should().Be(5500m);
        result!.Diario.Should().HaveCount(3);
        result!.Diario[0].Data.Should().Be(new DateOnly(2024, 10, 05));
        result!.Diario[0].Entradas.Should().Be(5000m);
        result!.Diario[0].SaldoAcumulado.Should().Be(5000m);
        result!.Diario[1].Data.Should().Be(new DateOnly(2024, 10, 10));
        result!.Diario[1].Saidas.Should().Be(500m);
        result!.Diario[1].SaldoAcumulado.Should().Be(4500m);
        result!.Diario[2].Data.Should().Be(new DateOnly(2024, 10, 20));
        result!.Diario[2].Entradas.Should().Be(1000m);
        result!.Diario[2].SaldoAcumulado.Should().Be(5500m);
    }

    [Fact]
    public async Task FluxoCaixa_ComTransferenciasEInvestimentos_NaoEntramNoFluxo()
    {
        await ClearDatabaseAsync();
        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);
        context.Transacoes.AddRange(
            new Transacao(new DateOnly(2024, 10, 05), "Salário", new Dinheiro(5000m, "BRL"), NaturezaTransacao.Receita, conta, Contexto.Pessoal, Pessoa.Eu),
            new Transacao(new DateOnly(2024, 10, 10), "Transf", new Dinheiro(1000m, "BRL"), NaturezaTransacao.Transferencia, conta, Contexto.Pessoal, Pessoa.Eu),
            new Transacao(new DateOnly(2024, 10, 15), "Aporte", new Dinheiro(2000m, "BRL"), NaturezaTransacao.Investimento, conta, Contexto.Investimentos, Pessoa.Eu));
        await context.SaveChangesAsync();

        var response = await Client.GetAsync("/api/relatorios/fluxo-caixa?dataInicio=2024-10-01&dataFim=2024-10-31");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<FluxoCaixaResponse>();
        result!.TotalEntradas.Should().Be(5000m);
        result!.TotalSaidas.Should().Be(0m);
        result!.SaldoFinal.Should().Be(5000m);
        result!.Diario.Should().HaveCount(1); // só o dia do salário
    }
}