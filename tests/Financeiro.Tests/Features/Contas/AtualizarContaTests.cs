using System.Net;
using System.Net.Http.Json;
using Financeiro.Domain.Entities;
using Financeiro.Domain.Enums;
using Financeiro.Features.Contas;
using FluentAssertions;
using Xunit;

namespace Financeiro.Tests.Features.Contas;

public class AtualizarContaTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Atualizar_ContaExistente_RetornaSucesso()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var context = await GetDbContextAsync();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, "Nu Pagamentos");
        context.Contas.Add(conta);
        await context.SaveChangesAsync();

        var request = new Atualizar.FeatureRequest(conta.Id, "Nubank Conta PJ");

        // Act
        var response = await Client.PutAsJsonAsync($"/api/contas/{conta.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Atualizar.FeatureResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(conta.Id);
        result.Nome.Should().Be("Nubank Conta PJ");
        result.Tipo.Should().Be(TipoConta.ContaCorrente);
        result.Instituicao.Should().Be("Nu Pagamentos");
    }

    [Fact]
    public async Task Atualizar_ContaInexistente_RetornaNotFound()
    {
        // Arrange
        await ClearDatabaseAsync();
        var idInexistente = Guid.NewGuid();
        var request = new Atualizar.FeatureRequest(idInexistente, "Novo Nome");

        // Act
        var response = await Client.PutAsJsonAsync($"/api/contas/{idInexistente}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Atualizar_NomeVazio_RetornaErroValidacao()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var context = await GetDbContextAsync();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);
        await context.SaveChangesAsync();

        var request = new Atualizar.FeatureRequest(conta.Id, "");

        // Act
        var response = await Client.PutAsJsonAsync($"/api/contas/{conta.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Atualizar_NomeMuitoLongo_RetornaErroValidacao()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var context = await GetDbContextAsync();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);
        await context.SaveChangesAsync();

        var request = new Atualizar.FeatureRequest(conta.Id, new string('A', 101));

        // Act
        var response = await Client.PutAsJsonAsync($"/api/contas/{conta.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Atualizar_ContaInativa_RetornaSucesso()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var context = await GetDbContextAsync();
        var conta = new Conta("Antiga", TipoConta.ContaCorrente, null);
        conta.Desativar();
        context.Contas.Add(conta);
        await context.SaveChangesAsync();

        var request = new Atualizar.FeatureRequest(conta.Id, "Antiga Renomeada");

        // Act
        var response = await Client.PutAsJsonAsync($"/api/contas/{conta.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Atualizar.FeatureResponse>();
        result.Should().NotBeNull();
        result!.Nome.Should().Be("Antiga Renomeada");
        result.Ativa.Should().BeFalse();
    }
}
