using System.Net;
using System.Net.Http.Json;
using Financeiro.Domain.Entities;
using Financeiro.Domain.Enums;
using Financeiro.Domain.ValueObjects;
using Financeiro.Features.Transacoes;
using Financeiro.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Financeiro.Tests.Features.Transacoes;

public class FilaRevisaoTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    // ---------- ListarPendentes ----------

    [Fact]
    public async Task ListarPendentes_SemTransacoes_RetornaVazio()
    {
        // Arrange
        await ClearDatabaseAsync();

        // Act
        var response = await Client.GetAsync("/api/transacoes/pendentes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TransacaoResponse[]>();
        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }

    [Fact]
    public async Task ListarPendentes_ComMistas_RetornaApenasNaoClassificadas()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        var categoria = new Categoria("Alimentação");
        context.Contas.Add(conta);
        context.Categorias.Add(categoria);

        var pendente = new Transacao(new DateOnly(2024, 10, 10), "Mercado", new Dinheiro(100m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu);
        var classificada = new Transacao(new DateOnly(2024, 10, 11), "Farmácia", new Dinheiro(50m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu);
        classificada.Classificar(categoria, Contexto.Pessoal, Pessoa.Eu, Array.Empty<Tag>());

        context.Transacoes.AddRange(pendente, classificada);
        await context.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/transacoes/pendentes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TransacaoResponse[]>();
        result.Should().NotBeNull();
        result!.Should().HaveCount(1);
        result![0].Id.Should().Be(pendente.Id);
        result![0].Classificada.Should().BeFalse();
    }

    [Fact]
    public async Task ListarPendentes_OrdenaPorDataDecrescente()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);

        var t1 = new Transacao(new DateOnly(2024, 10, 05), "Antiga", new Dinheiro(10m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu);
        var t2 = new Transacao(new DateOnly(2024, 10, 15), "Recente", new Dinheiro(20m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu);
        var t3 = new Transacao(new DateOnly(2024, 10, 10), "Meio", new Dinheiro(30m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu);

        context.Transacoes.AddRange(t1, t2, t3);
        await context.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/transacoes/pendentes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TransacaoResponse[]>();
        result.Should().NotBeNull();
        result!.Should().HaveCount(3);
        result![0].Data.Should().Be(new DateOnly(2024, 10, 15));
        result![1].Data.Should().Be(new DateOnly(2024, 10, 10));
        result![2].Data.Should().Be(new DateOnly(2024, 10, 05));
    }

    // ---------- Classificar ----------

    [Fact]
    public async Task Classificar_TransacaoExistente_MarcaComoClassificada()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        var categoria = new Categoria("Alimentação");
        context.Contas.Add(conta);
        context.Categorias.Add(categoria);

        var transacao = new Transacao(new DateOnly(2024, 10, 10), "Mercado", new Dinheiro(100m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu);
        context.Transacoes.Add(transacao);
        await context.SaveChangesAsync();

        var request = new Classificar.FeatureRequest(
            Id: transacao.Id,
            CategoriaId: categoria.Id,
            Contexto: Contexto.Pessoal,
            Pessoa: Pessoa.Eu,
            TagIds: null);

        // Act
        var response = await Client.PutAsJsonAsync($"/api/transacoes/{transacao.Id}/classificar", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TransacaoResponse>();
        result.Should().NotBeNull();
        result!.Classificada.Should().BeTrue();
        result!.CategoriaId.Should().Be(categoria.Id);
        result!.Contexto.Should().Be(Contexto.Pessoal);
        result!.Pessoa.Should().Be(Pessoa.Eu);
        result!.ClassificadaEm.Should().NotBeNull();
    }

    [Fact]
    public async Task Classificar_TransacaoInexistente_RetornaNotFound()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var categoria = new Categoria("Alimentação");
        context.Categorias.Add(categoria);
        await context.SaveChangesAsync();

        var request = new Classificar.FeatureRequest(
            Id: Guid.NewGuid(),
            CategoriaId: categoria.Id,
            Contexto: Contexto.Pessoal,
            Pessoa: Pessoa.Eu,
            TagIds: null);

        // Act
        var response = await Client.PutAsJsonAsync($"/api/transacoes/{request.Id}/classificar", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Classificar_CategoriaInexistente_RetornaNotFound()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);

        var transacao = new Transacao(new DateOnly(2024, 10, 10), "Mercado", new Dinheiro(100m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu);
        context.Transacoes.Add(transacao);
        await context.SaveChangesAsync();

        var request = new Classificar.FeatureRequest(
            Id: transacao.Id,
            CategoriaId: Guid.NewGuid(),
            Contexto: Contexto.Pessoal,
            Pessoa: Pessoa.Eu,
            TagIds: null);

        // Act
        var response = await Client.PutAsJsonAsync($"/api/transacoes/{transacao.Id}/classificar", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Classificar_ComTags_AssociaTags()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        var categoria = new Categoria("Alimentação");
        var tag1 = new Tag("viagem");
        var tag2 = new Tag("trabalho");
        context.Contas.Add(conta);
        context.Categorias.Add(categoria);
        context.Tags.AddRange(tag1, tag2);

        var transacao = new Transacao(new DateOnly(2024, 10, 10), "Mercado", new Dinheiro(100m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu);
        context.Transacoes.Add(transacao);
        await context.SaveChangesAsync();

        var request = new Classificar.FeatureRequest(
            Id: transacao.Id,
            CategoriaId: categoria.Id,
            Contexto: Contexto.Pessoal,
            Pessoa: Pessoa.Eu,
            TagIds: new List<Guid> { tag1.Id, tag2.Id });

        // Act
        var response = await Client.PutAsJsonAsync($"/api/transacoes/{transacao.Id}/classificar", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var verifyScope = CreateAsyncScope();
        var verify = verifyScope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var transacaoNoBanco = await verify.Transacoes
            .Include(t => t.Tags)
            .SingleAsync(t => t.Id == transacao.Id);

        transacaoNoBanco.Tags.Should().HaveCount(2);
        transacaoNoBanco.Tags.Select(t => t.Id).Should().BeEquivalentTo(new[] { tag1.Id, tag2.Id });
    }

    [Fact]
    public async Task Classificar_SemTags_ClassificaSemTags()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        var categoria = new Categoria("Alimentação");
        context.Contas.Add(conta);
        context.Categorias.Add(categoria);

        var transacao = new Transacao(new DateOnly(2024, 10, 10), "Mercado", new Dinheiro(100m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu);
        context.Transacoes.Add(transacao);
        await context.SaveChangesAsync();

        var request = new Classificar.FeatureRequest(
            Id: transacao.Id,
            CategoriaId: categoria.Id,
            Contexto: Contexto.Pessoal,
            Pessoa: Pessoa.Eu,
            TagIds: null);

        // Act
        var response = await Client.PutAsJsonAsync($"/api/transacoes/{transacao.Id}/classificar", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var verifyScope = CreateAsyncScope();
        var verify = verifyScope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var transacaoNoBanco = await verify.Transacoes
            .Include(t => t.Tags)
            .SingleAsync(t => t.Id == transacao.Id);

        transacaoNoBanco.Classificada.Should().BeTrue();
        transacaoNoBanco.Tags.Should().BeEmpty();
    }

    [Fact]
    public async Task Classificar_CategoriaIdVazio_RetornaBadRequest()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);

        var transacao = new Transacao(new DateOnly(2024, 10, 10), "Mercado", new Dinheiro(100m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu);
        context.Transacoes.Add(transacao);
        await context.SaveChangesAsync();

        var request = new Classificar.FeatureRequest(
            Id: transacao.Id,
            CategoriaId: Guid.Empty,
            Contexto: Contexto.Pessoal,
            Pessoa: Pessoa.Eu,
            TagIds: null);

        // Act
        var response = await Client.PutAsJsonAsync($"/api/transacoes/{transacao.Id}/classificar", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---------- Desclassificar ----------

    [Fact]
    public async Task Desclassificar_TransacaoClassificada_RemoveClassificacao()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        var categoria = new Categoria("Alimentação");
        context.Contas.Add(conta);
        context.Categorias.Add(categoria);

        var transacao = new Transacao(new DateOnly(2024, 10, 10), "Mercado", new Dinheiro(100m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu);
        transacao.Classificar(categoria, Contexto.Pessoal, Pessoa.Eu, Array.Empty<Tag>());
        context.Transacoes.Add(transacao);
        await context.SaveChangesAsync();

        // Act
        var response = await Client.PutAsJsonAsync(
            $"/api/transacoes/{transacao.Id}/desclassificar",
            new Desclassificar.FeatureRequest(transacao.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TransacaoResponse>();
        result.Should().NotBeNull();
        result!.Classificada.Should().BeFalse();
        result!.CategoriaId.Should().BeNull();
        result!.ClassificadaEm.Should().BeNull();
    }

    [Fact]
    public async Task Desclassificar_TransacaoInexistente_RetornaNotFound()
    {
        // Arrange
        await ClearDatabaseAsync();
        var id = Guid.NewGuid();

        // Act
        var response = await Client.PutAsJsonAsync(
            $"/api/transacoes/{id}/desclassificar",
            new Desclassificar.FeatureRequest(id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Desclassificar_TransacaoJaPendente_RetornaSucesso()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);

        var transacao = new Transacao(new DateOnly(2024, 10, 10), "Mercado", new Dinheiro(100m, "BRL"), NaturezaTransacao.Despesa, conta, Contexto.Pessoal, Pessoa.Eu);
        context.Transacoes.Add(transacao);
        await context.SaveChangesAsync();

        // Act
        var response = await Client.PutAsJsonAsync(
            $"/api/transacoes/{transacao.Id}/desclassificar",
            new Desclassificar.FeatureRequest(transacao.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TransacaoResponse>();
        result.Should().NotBeNull();
        result!.Classificada.Should().BeFalse();
        result!.CategoriaId.Should().BeNull();
        result!.ClassificadaEm.Should().BeNull();
    }
}