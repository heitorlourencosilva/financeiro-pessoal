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

public class FiltrosTransacoesTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    // --- Período ---

    [Fact]
    public async Task Listar_ComDataInicio_RetornaApenasAPartirDaData()
    {
        await ClearDatabaseAsync();
        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);
        context.Transacoes.AddRange(
            new Transacao(new DateOnly(2024, 10, 05), "Antes", new Dinheiro(10m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu),
            new Transacao(new DateOnly(2024, 10, 10), "Limite", new Dinheiro(20m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu),
            new Transacao(new DateOnly(2024, 10, 15), "Depois", new Dinheiro(30m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu));
        await context.SaveChangesAsync();

        var response = await Client.GetAsync("/api/transacoes?dataInicio=2024-10-10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TransacaoResponse[]>();
        result!.Should().HaveCount(2);
        result!.Select(t => t.Descricao).Should().BeEquivalentTo(new[] { "Limite", "Depois" });
    }

    [Fact]
    public async Task Listar_ComDataFim_RetornaApenasAteAData()
    {
        await ClearDatabaseAsync();
        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);
        context.Transacoes.AddRange(
            new Transacao(new DateOnly(2024, 10, 05), "Antes", new Dinheiro(10m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu),
            new Transacao(new DateOnly(2024, 10, 10), "Limite", new Dinheiro(20m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu),
            new Transacao(new DateOnly(2024, 10, 15), "Depois", new Dinheiro(30m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu));
        await context.SaveChangesAsync();

        var response = await Client.GetAsync("/api/transacoes?dataFim=2024-10-10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TransacaoResponse[]>();
        result!.Should().HaveCount(2);
        result!.Select(t => t.Descricao).Should().BeEquivalentTo(new[] { "Antes", "Limite" });
    }

    [Fact]
    public async Task Listar_ComIntervalo_RetornaApenasDoIntervalo()
    {
        await ClearDatabaseAsync();
        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);
        context.Transacoes.AddRange(
            new Transacao(new DateOnly(2024, 10, 01), "Fora", new Dinheiro(10m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu),
            new Transacao(new DateOnly(2024, 10, 10), "Dentro1", new Dinheiro(20m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu),
            new Transacao(new DateOnly(2024, 10, 20), "Dentro2", new Dinheiro(30m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu),
            new Transacao(new DateOnly(2024, 10, 31), "Fora", new Dinheiro(40m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu));
        await context.SaveChangesAsync();

        var response = await Client.GetAsync("/api/transacoes?dataInicio=2024-10-10&dataFim=2024-10-20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TransacaoResponse[]>();
        result!.Should().HaveCount(2);
        result!.Select(t => t.Descricao).Should().BeEquivalentTo(new[] { "Dentro1", "Dentro2" });
    }

    // --- Categoria ---

    [Fact]
    public async Task Listar_ComCategoriaId_RetornaApenasDaCategoria()
    {
        await ClearDatabaseAsync();
        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        var cat = new Categoria("Alimentação", null);
        context.Contas.Add(conta);
        context.Categorias.Add(cat);

        var t1 = new Transacao(new DateOnly(2024, 10, 10), "Mercado", new Dinheiro(100m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu);
        t1.Classificar(cat, Contexto.Pessoal, Pessoa.Eu);
        var t2 = new Transacao(new DateOnly(2024, 10, 11), "SemCategoria", new Dinheiro(50m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu);
        context.Transacoes.AddRange(t1, t2);
        await context.SaveChangesAsync();

        var response = await Client.GetAsync($"/api/transacoes?categoriaId={cat.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TransacaoResponse[]>();
        result!.Should().HaveCount(1);
        result![0].Descricao.Should().Be("Mercado");
    }

    // --- Contexto ---

    [Fact]
    public async Task Listar_ComContexto_RetornaApenasDoContexto()
    {
        await ClearDatabaseAsync();
        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);
        context.Transacoes.AddRange(
            new Transacao(new DateOnly(2024, 10, 10), "Pessoal", new Dinheiro(10m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu),
            new Transacao(new DateOnly(2024, 10, 11), "YouTube", new Dinheiro(20m, "BRL"), NaturezaTransacao.Receita, conta, Contexto.YouTube, Pessoa.Eu));
        await context.SaveChangesAsync();

        var response = await Client.GetAsync("/api/transacoes?contexto=YouTube");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TransacaoResponse[]>();
        result!.Should().HaveCount(1);
        result![0].Descricao.Should().Be("YouTube");
    }

    // --- Pessoa ---

    [Fact]
    public async Task Listar_ComPessoa_RetornaApenasDaPessoa()
    {
        await ClearDatabaseAsync();
        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);
        context.Transacoes.AddRange(
            new Transacao(new DateOnly(2024, 10, 10), "Eu", new Dinheiro(10m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu),
            new Transacao(new DateOnly(2024, 10, 11), "Conjuge", new Dinheiro(20m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Familiar, Pessoa.Conjuge));
        await context.SaveChangesAsync();

        var response = await Client.GetAsync("/api/transacoes?pessoa=Conjuge");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TransacaoResponse[]>();
        result!.Should().HaveCount(1);
        result![0].Descricao.Should().Be("Conjuge");
    }

    // --- Tag ---

    [Fact]
    public async Task Listar_ComTagId_RetornaApenasComATag()
    {
        await ClearDatabaseAsync();
        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        var tag = new Tag("viagem");
        context.Contas.Add(conta);
        context.Tags.Add(tag);

        var t1 = new Transacao(new DateOnly(2024, 10, 10), "ComTag", new Dinheiro(10m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu);
        t1.AdicionarTag(tag);
        var t2 = new Transacao(new DateOnly(2024, 10, 11), "SemTag", new Dinheiro(20m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu);
        context.Transacoes.AddRange(t1, t2);
        await context.SaveChangesAsync();

        var response = await Client.GetAsync($"/api/transacoes?tagId={tag.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TransacaoResponse[]>();
        result!.Should().HaveCount(1);
        result![0].Descricao.Should().Be("ComTag");
    }

    // --- Combinação ---

    [Fact]
    public async Task Listar_ComContextoEPeriodo_RetornaApenasCombinacao()
    {
        await ClearDatabaseAsync();
        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);
        context.Transacoes.AddRange(
            new Transacao(new DateOnly(2024, 10, 05), "YouTubeAntes", new Dinheiro(10m, "BRL"), NaturezaTransacao.Receita, conta, Contexto.YouTube, Pessoa.Eu),
            new Transacao(new DateOnly(2024, 10, 15), "YouTubeDentro", new Dinheiro(20m, "BRL"), NaturezaTransacao.Receita, conta, Contexto.YouTube, Pessoa.Eu),
            new Transacao(new DateOnly(2024, 10, 15), "PessoalDentro", new Dinheiro(30m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu));
        await context.SaveChangesAsync();

        var response = await Client.GetAsync("/api/transacoes?contexto=YouTube&dataInicio=2024-10-10&dataFim=2024-10-20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TransacaoResponse[]>();
        result!.Should().HaveCount(1);
        result![0].Descricao.Should().Be("YouTubeDentro");
    }

    // --- Regressão: sem filtros ---

    [Fact]
    public async Task Listar_SemFiltros_RetornaTodas()
    {
        await ClearDatabaseAsync();
        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);
        context.Transacoes.AddRange(
            new Transacao(new DateOnly(2024, 10, 10), "A", new Dinheiro(10m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu),
            new Transacao(new DateOnly(2024, 10, 11), "B", new Dinheiro(20m, "BRL"), NaturezaTransacao.Receita, conta, Contexto.YouTube, Pessoa.Conjuge));
        await context.SaveChangesAsync();

        var response = await Client.GetAsync("/api/transacoes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TransacaoResponse[]>();
        result!.Should().HaveCount(2);
    }
}