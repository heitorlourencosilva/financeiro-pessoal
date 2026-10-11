using System.Net;
using System.Net.Http.Json;
using Financeiro.Domain.Entities;
using Financeiro.Domain.Enums;
using Financeiro.Features.Contas;
using FluentAssertions;
using Xunit;

namespace Financeiro.Tests.Features.Contas;

public class ObterContaTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Obter_ContaExistente_RetornaConta()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var context = await GetDbContextAsync();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, "Nu Pagamentos S.A.");
        context.Contas.Add(conta);
        await context.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/contas/{conta.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Obter.FeatureResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(conta.Id);
        result.Nome.Should().Be("Nubank");
        result.Tipo.Should().Be(TipoConta.ContaCorrente);
        result.Instituicao.Should().Be("Nu Pagamentos S.A.");
        result.Ativa.Should().BeTrue();
    }

    [Fact]
    public async Task Obter_ContaInexistente_RetornaNotFound()
    {
        // Arrange
        await ClearDatabaseAsync();
        var idInexistente = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/contas/{idInexistente}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Obter_ContaInativa_RetornaConta()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var context = await GetDbContextAsync();
        var conta = new Conta("Antiga", TipoConta.ContaCorrente, null);
        conta.Desativar();
        context.Contas.Add(conta);
        await context.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/contas/{conta.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Obter.FeatureResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(conta.Id);
        result.Ativa.Should().BeFalse();
    }

    [Fact]
    public async Task Obter_ContaSemInstituicao_RetornaConta()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var context = await GetDbContextAsync();
        var conta = new Conta("Carteira Digital", TipoConta.CarteiraDigital, null);
        context.Contas.Add(conta);
        await context.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/contas/{conta.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Obter.FeatureResponse>();
        result.Should().NotBeNull();
        result!.Instituicao.Should().BeNull();
    }
}
