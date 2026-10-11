using System.Net;
using System.Net.Http.Json;
using Financeiro.Domain.Entities;
using Financeiro.Domain.Enums;
using Financeiro.Features.Contas;
using FluentAssertions;
using Xunit;

namespace Financeiro.Tests.Features.Contas;

public class DesativarContaTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Desativar_ContaAtiva_RetornaSucesso()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var context = await GetDbContextAsync();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, "Nu Pagamentos");
        context.Contas.Add(conta);
        await context.SaveChangesAsync();

        // Act
        var response = await Client.DeleteAsync($"/api/contas/{conta.Id}/desativar");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Desativar.FeatureResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(conta.Id);
        result.Ativa.Should().BeFalse();
    }

    [Fact]
    public async Task Desativar_ContaInexistente_RetornaNotFound()
    {
        // Arrange
        await ClearDatabaseAsync();
        var idInexistente = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/contas/{idInexistente}/desativar");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Desativar_ContaJaInativa_RetornaSucesso()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var context = await GetDbContextAsync();
        var conta = new Conta("Antiga", TipoConta.ContaCorrente, null);
        conta.Desativar();
        context.Contas.Add(conta);
        await context.SaveChangesAsync();

        // Act
        var response = await Client.DeleteAsync($"/api/contas/{conta.Id}/desativar");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Desativar.FeatureResponse>();
        result.Should().NotBeNull();
        result!.Ativa.Should().BeFalse();
    }

    [Fact]
    public async Task Desativar_VerificaPersistencia_ContaFicaInativa()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var contextArrange = await GetDbContextAsync();
        var conta = new Conta("Nubank", TipoConta.ContaCorrente, null);
        contextArrange.Contas.Add(conta);
        await contextArrange.SaveChangesAsync();
        var contaId = conta.Id;

        // Act
        await Client.DeleteAsync($"/api/contas/{contaId}/desativar");

        // Assert
        await using var contextAssert = await GetDbContextAsync();
        var contaDesativada = await contextAssert.Contas.FindAsync(contaId);
        contaDesativada.Should().NotBeNull();
        contaDesativada!.Ativa.Should().BeFalse();
    }
}
