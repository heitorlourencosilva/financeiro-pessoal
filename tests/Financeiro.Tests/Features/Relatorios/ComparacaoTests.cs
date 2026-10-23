using System.Net;
using System.Net.Http.Json;
using Financeiro.Domain.Entities;
using Financeiro.Domain.Enums;
using Financeiro.Domain.ValueObjects;
using Financeiro.Features.Relatorios.Comparacao;
using Financeiro.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Financeiro.Tests.Features.Relatorios;

public class ComparacaoTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Comparacao_SemPeriodo_RetornaBadRequest()
    {
        await ClearDatabaseAsync();

        var response = await Client.GetAsync("/api/relatorios/comparacao");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Comparacao_SemTransacoes_RetornaTudoZerado()
    {
        await ClearDatabaseAsync();

        var response = await Client.GetAsync(
            "/api/relatorios/comparacao?periodoAInicio=2024-09-01&periodoAFim=2024-09-30&periodoBInicio=2024-10-01&periodoBFim=2024-10-31");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ComparacaoResponse>();
        result!.PeriodoA.TotalReceitas.Should().Be(0m);
        result!.PeriodoB.TotalReceitas.Should().Be(0m);
        result!.Receitas.Should().BeEmpty();
        result!.Despesas.Should().BeEmpty();
    }

    [Fact]
    public async Task Comparacao_ReceitasEmCategorias_CalculaDiferencaEVariacao()
    {
        await ClearDatabaseAsync();
        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        var catSalario = new Categoria("Salário", null);
        var catFreela = new Categoria("Freelance", null);
        context.Contas.Add(conta);
        context.Categorias.AddRange(catSalario, catFreela);

        // Período A (set): salário 5000
        var a1 = new Transacao(new DateOnly(2024, 09, 05), "Salário", new Dinheiro(5000m, "BRL"), NaturezaTransacao.Receita, conta, Contexto.Pessoal, Pessoa.Eu);
        a1.Classificar(catSalario, Contexto.Pessoal, Pessoa.Eu);

        // Período B (out): salário 5000 + freela 2000
        var b1 = new Transacao(new DateOnly(2024, 10, 05), "Salário", new Dinheiro(5000m, "BRL"), NaturezaTransacao.Receita, conta, Contexto.Pessoal, Pessoa.Eu);
        b1.Classificar(catSalario, Contexto.Pessoal, Pessoa.Eu);
        var b2 = new Transacao(new DateOnly(2024, 10, 10), "Freela", new Dinheiro(2000m, "BRL"), NaturezaTransacao.Receita, conta, Contexto.YouTube, Pessoa.Eu);
        b2.Classificar(catFreela, Contexto.YouTube, Pessoa.Eu);

        context.Transacoes.AddRange(a1, b1, b2);
        await context.SaveChangesAsync();

        var response = await Client.GetAsync(
            "/api/relatorios/comparacao?periodoAInicio=2024-09-01&periodoAFim=2024-09-30&periodoBInicio=2024-10-01&periodoBFim=2024-10-31");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ComparacaoResponse>();

        result!.PeriodoA.TotalReceitas.Should().Be(5000m);
        result!.PeriodoB.TotalReceitas.Should().Be(7000m);
        result!.PeriodoA.Saldo.Should().Be(5000m);
        result!.PeriodoB.Saldo.Should().Be(7000m);

        var linhaSalario = result!.Receitas.Single(r => r.CategoriaNome == "Salário");
        linhaSalario.TotalA.Should().Be(5000m);
        linhaSalario.TotalB.Should().Be(5000m);
        linhaSalario.Diferenca.Should().Be(0m);
        linhaSalario.VariacaoPercentual.Should().Be(0m);

        var linhaFreela = result!.Receitas.Single(r => r.CategoriaNome == "Freelance");
        linhaFreela.TotalA.Should().Be(0m);
        linhaFreela.TotalB.Should().Be(2000m);
        linhaFreela.VariacaoPercentual.Should().BeNull(); // sem base em A
    }

    [Fact]
    public async Task Comparacao_CategoriaSoEmA_TotalBZeroEVariacaoNegativa()
    {
        await ClearDatabaseAsync();
        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        var cat = new Categoria("Lazer", null);
        context.Contas.Add(conta);
        context.Categorias.Add(cat);

        var a = new Transacao(new DateOnly(2024, 09, 05), "Cinema", new Dinheiro(100m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu);
        a.Classificar(cat, Contexto.Pessoal, Pessoa.Eu);
        context.Transacoes.Add(a);
        await context.SaveChangesAsync();

        var response = await Client.GetAsync(
            "/api/relatorios/comparacao?periodoAInicio=2024-09-01&periodoAFim=2024-09-30&periodoBInicio=2024-10-01&periodoBFim=2024-10-31");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ComparacaoResponse>();
        var linha = result!.Despesas.Single();
        linha.TotalA.Should().Be(100m);
        linha.TotalB.Should().Be(0m);
        linha.VariacaoPercentual.Should().Be(-100m);
    }

    [Fact]
    public async Task Comparacao_ComNeutros_NaoAparecem()
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

        var response = await Client.GetAsync(
            "/api/relatorios/comparacao?periodoAInicio=2024-09-01&periodoAFim=2024-09-30&periodoBInicio=2024-10-01&periodoBFim=2024-10-31");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ComparacaoResponse>();
        result!.PeriodoB.TotalReceitas.Should().Be(5000m);
        result!.PeriodoB.TotalDespesas.Should().Be(0m); // neutros fora
        result!.Despesas.Should().BeEmpty();
    }

    [Fact]
    public async Task Comparacao_PeriodosIguais_DiferencaZero()
    {
        await ClearDatabaseAsync();
        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        var cat = new Categoria("Salário", null);
        context.Contas.Add(conta);
        context.Categorias.Add(cat);

        var t = new Transacao(new DateOnly(2024, 10, 05), "Salário", new Dinheiro(5000m, "BRL"), NaturezaTransacao.Receita, conta, Contexto.Pessoal, Pessoa.Eu);
        t.Classificar(cat, Contexto.Pessoal, Pessoa.Eu);
        context.Transacoes.Add(t);
        await context.SaveChangesAsync();

        var response = await Client.GetAsync(
            "/api/relatorios/comparacao?periodoAInicio=2024-10-01&periodoAFim=2024-10-31&periodoBInicio=2024-10-01&periodoBFim=2024-10-31");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ComparacaoResponse>();
        var linha = result!.Receitas.Single();
        linha.Diferenca.Should().Be(0m);
        linha.VariacaoPercentual.Should().Be(0m);
    }
}