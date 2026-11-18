using System.Net;
using System.Net.Http.Json;
using Financeiro.Domain.Entities;
using Financeiro.Features.Categorias;
using Financeiro.Features.Tags;
using Financeiro.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Financeiro.Tests.Features.Transacoes;

public class OpcoesFiltrosTransacoesTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task ListarCategorias_RetornaApenasCategoriasAtivas()
    {
        await ClearDatabaseAsync();
        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var ativa = new Categoria("Alimentação");
        var inativa = new Categoria("Viagem");
        inativa.Desativar();
        context.Categorias.AddRange(ativa, inativa);
        await context.SaveChangesAsync();

        var response = await Client.GetAsync("/api/categorias");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ListarCategoriaResponse[]>();
        result.Should().ContainSingle();
        result![0].Nome.Should().Be("Alimentação");
    }

    [Fact]
    public async Task ListarTags_RetornaTagsOrdenadasPorNome()
    {
        await ClearDatabaseAsync();
        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        context.Tags.AddRange(new Tag("viagem"), new Tag("casa"));
        await context.SaveChangesAsync();

        var response = await Client.GetAsync("/api/tags");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ListarTagResponse[]>();
        result!.Select(tag => tag.Nome).Should().ContainInOrder("casa", "viagem");
    }

    public record ListarCategoriaResponse(Guid Id, string Nome, Guid? CategoriaPaiId);
    public record ListarTagResponse(Guid Id, string Nome);
}
