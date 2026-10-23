using System.Net;
using System.Net.Http.Json;
using Financeiro.Domain.Entities;
using Financeiro.Domain.Enums;
using Financeiro.Domain.ValueObjects;
using Financeiro.Features.Relatorios.Composicao;
using Financeiro.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Financeiro.Tests.Features.Relatorios;

public class ComposicaoTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Composicao_SemTransacoes_RetornaListasVazias()
    {
        await ClearDatabaseAsync();

        var response = await Client.GetAsync("/api/relatorios/composicao");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ComposicaoResponse>();
        result!.Receitas.Should().BeEmpty();
        result!.Despesas.Should().BeEmpty();
    }

    [Fact]
    public async Task Composicao_ComReceitasEmCategoriasDiferentes_AgrupaEOrdenaPorTotalDesc()
    {
        await ClearDatabaseAsync();
        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        var catSalario = new Categoria("Salário", null);
        var catFreela = new Categoria("Freelance", null);
        context.Contas.Add(conta);
        context.Categorias.AddRange(catSalario, catFreela);

        var t1 = new Transacao(new DateOnly(2024, 10, 05), "Salário Out", new Dinheiro(5000m, "BRL"), NaturezaTransacao.Receita, conta, Contexto.Pessoal, Pessoa.Eu);
        t1.Classificar(catSalario, Contexto.Pessoal, Pessoa.Eu);
        var t2 = new Transacao(new DateOnly(2024, 10, 10), "Freela", new Dinheiro(2000m, "BRL"), NaturezaTransacao.Receita, conta, Contexto.YouTube, Pessoa.Eu);
        t2.Classificar(catFreela, Contexto.YouTube, Pessoa.Eu);
        context.Transacoes.AddRange(t1, t2);
        await context.SaveChangesAsync();

        var response = await Client.GetAsync("/api/relatorios/composicao");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ComposicaoResponse>();
        result!.Receitas.Should().HaveCount(2);
        result!.Receitas[0].CategoriaNome.Should().Be("Salário");
        result!.Receitas[0].Total.Should().Be(5000m);
        result!.Receitas[1].CategoriaNome.Should().Be("Freelance");
        result!.Receitas[1].Total.Should().Be(2000m);
        result!.Despesas.Should().BeEmpty();
    }

    [Fact]
    public async Task Composicao_ComDespesasEmTresCategorias_AgrupaCorretamente()
    {
        await ClearDatabaseAsync();
        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        var catAlim = new Categoria("Alimentação", null);
        var catTransp = new Categoria("Transporte", null);
        var catLazer = new Categoria("Lazer", null);
        context.Contas.Add(conta);
        context.Categorias.AddRange(catAlim, catTransp, catLazer);

        var t1 = new Transacao(new DateOnly(2024, 10, 05), "Mercado", new Dinheiro(300m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Familiar, Pessoa.Eu);
        t1.Classificar(catAlim, Contexto.Familiar, Pessoa.Eu);
        var t2 = new Transacao(new DateOnly(2024, 10, 06), "Uber", new Dinheiro(100m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu);
        t2.Classificar(catTransp, Contexto.Pessoal, Pessoa.Eu);
        var t3 = new Transacao(new DateOnly(2024, 10, 07), "Cinema", new Dinheiro(50m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu);
        t3.Classificar(catLazer, Contexto.Pessoal, Pessoa.Eu);
        var t4 = new Transacao(new DateOnly(2024, 10, 08), "Padaria", new Dinheiro(200m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Familiar, Pessoa.Eu);
        t4.Classificar(catAlim, Contexto.Familiar, Pessoa.Eu);
        context.Transacoes.AddRange(t1, t2, t3, t4);
        await context.SaveChangesAsync();

        var response = await Client.GetAsync("/api/relatorios/composicao");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ComposicaoResponse>();
        result!.Despesas.Should().HaveCount(3);
        result!.Despesas[0].CategoriaNome.Should().Be("Alimentação");
        result!.Despesas[0].Total.Should().Be(500m);
        result!.Despesas[1].CategoriaNome.Should().Be("Transporte");
        result!.Despesas[1].Total.Should().Be(100m);
        result!.Despesas[2].CategoriaNome.Should().Be("Lazer");
        result!.Despesas[2].Total.Should().Be(50m);
    }

    [Fact]
    public async Task Composicao_TransacaoSemCategoria_ApareceComoSemCategoria()
    {
        await ClearDatabaseAsync();
        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);
        context.Transacoes.Add(
            new Transacao(new DateOnly(2024, 10, 05), "Mistério", new Dinheiro(150m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu));
        await context.SaveChangesAsync();

        var response = await Client.GetAsync("/api/relatorios/composicao");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ComposicaoResponse>();
        result!.Despesas.Should().HaveCount(1);
        result!.Despesas[0].CategoriaId.Should().BeNull();
        result!.Despesas[0].CategoriaNome.Should().Be("Sem categoria");
        result!.Despesas[0].Total.Should().Be(150m);
    }

    [Fact]
    public async Task Composicao_ComTransferenciasEInvestimentos_NaoAparecem()
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

        var response = await Client.GetAsync("/api/relatorios/composicao");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ComposicaoResponse>();
        result!.Receitas.Should().HaveCount(1);
        result!.Receitas.Sum(r => r.Total).Should().Be(5000m);
        result!.Despesas.Should().BeEmpty(); // Transferencia/Investimento não são Despesa
    }

    [Fact]
    public async Task Composicao_ComPeriodo_FiltraTransacoes()
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

        var response = await Client.GetAsync("/api/relatorios/composicao?dataInicio=2024-10-10&dataFim=2024-10-20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ComposicaoResponse>();
        result!.Despesas.Should().HaveCount(1);
        result!.Despesas[0].Total.Should().Be(200m);
    }
}
