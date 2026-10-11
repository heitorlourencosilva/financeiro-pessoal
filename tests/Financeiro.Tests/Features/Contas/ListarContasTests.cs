using System.Net;
using System.Net.Http.Json;
using Financeiro.Domain.Entities;
using Financeiro.Domain.Enums;
using Financeiro.Features.Contas;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Financeiro.Tests.Features.Contas;

public class ListarContasTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Listar_SemContas_RetornaArrayVazio()
    {
        // Arrange
        await ClearDatabaseAsync();

        // Act
        var response = await Client.GetAsync("/api/contas");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Listar.FeatureResponse[]>();
        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }

    [Fact]
    public async Task Listar_ComContas_RetornaTodasOrdenadasPorNome()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var context = await GetDbContextAsync();
        var conta1 = new Conta("Nubank", TipoConta.ContaCorrente, "Nu Pagamentos");
        var conta2 = new Conta("Carteira", TipoConta.CarteiraDigital, null);
        var conta3 = new Conta("BB", TipoConta.ContaCorrente, "Banco do Brasil");

        context.Contas.AddRange(conta1, conta2, conta3);
        await context.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/contas");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Listar.FeatureResponse[]>();
        result.Should().NotBeNull();
        result!.Should().HaveCount(3);
        result![0].Nome.Should().Be("BB");
        result[1].Nome.Should().Be("Carteira");
        result[2].Nome.Should().Be("Nubank");
    }

    [Fact]
    public async Task Listar_ContasAtivasEInativas_RetornaTodas()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var context = await GetDbContextAsync();
        var contaAtiva = new Conta("Ativa", TipoConta.ContaCorrente, null);
        var contaInativa = new Conta("Inativa", TipoConta.ContaCorrente, null);
        contaInativa.Desativar();

        context.Contas.AddRange(contaAtiva, contaInativa);
        await context.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/contas");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Listar.FeatureResponse[]>();
        result.Should().NotBeNull();
        result!.Should().HaveCount(2);
        result.Should().Contain(c => c.Nome == "Ativa" && c.Ativa);
        result.Should().Contain(c => c.Nome == "Inativa" && !c.Ativa);
    }

    [Fact]
    public async Task Listar_TodosTiposDeConta_RetornaTodos()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var context = await GetDbContextAsync();
        context.Contas.AddRange(
            new Conta("Corrente", TipoConta.ContaCorrente, null),
            new Conta("Poupança", TipoConta.Poupanca, null),
            new Conta("Investimento", TipoConta.Investimento, null),
            new Conta("Carteira Digital", TipoConta.CarteiraDigital, null)
        );
        await context.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/contas");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Listar.FeatureResponse[]>();
        result.Should().NotBeNull();
        result!.Should().HaveCount(4);
        result!.Select(c => c.Tipo).Should().Contain(new[]
        {
            TipoConta.ContaCorrente,
            TipoConta.Poupanca,
            TipoConta.Investimento,
            TipoConta.CarteiraDigital
        });
    }
}
