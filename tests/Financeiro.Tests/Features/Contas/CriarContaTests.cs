using System.Net;
using System.Net.Http.Json;
using Financeiro.Domain.Enums;
using Financeiro.Features.Contas;
using FluentAssertions;
using Xunit;

namespace Financeiro.Tests.Features.Contas;

public class CriarContaTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Criar_ContaValida_RetornaSucesso()
    {
        // Arrange
        var request = new Criar.FeatureRequest(
            Nome: "Nubank",
            Tipo: TipoConta.ContaCorrente,
            Instituicao: "Nu Pagamentos S.A.");

        // Act
        var response = await Client.PostAsJsonAsync("/api/contas", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Criar.FeatureResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
        result.Nome.Should().Be("Nubank");
        result.Tipo.Should().Be(TipoConta.ContaCorrente);
        result.Instituicao.Should().Be("Nu Pagamentos S.A.");
        result.Ativa.Should().BeTrue();
        result.CriadaEm.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Criar_NomeVazio_RetornaErroValidacao()
    {
        // Arrange
        var request = new Criar.FeatureRequest(
            Nome: "",
            Tipo: TipoConta.ContaCorrente,
            Instituicao: null);

        // Act
        var response = await Client.PostAsJsonAsync("/api/contas", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Criar_NomeMuitoLongo_RetornaErroValidacao()
    {
        // Arrange
        var request = new Criar.FeatureRequest(
            Nome: new string('A', 101),
            Tipo: TipoConta.ContaCorrente,
            Instituicao: null);

        // Act
        var response = await Client.PostAsJsonAsync("/api/contas", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Criar_InstituicaoMuitoLonga_RetornaErroValidacao()
    {
        // Arrange
        var request = new Criar.FeatureRequest(
            Nome: "Nubank",
            Tipo: TipoConta.ContaCorrente,
            Instituicao: new string('A', 101));

        // Act
        var response = await Client.PostAsJsonAsync("/api/contas", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Criar_SemInstituicao_RetornaSucesso()
    {
        // Arrange
        var request = new Criar.FeatureRequest(
            Nome: "Carteira Digital",
            Tipo: TipoConta.CarteiraDigital,
            Instituicao: null);

        // Act
        var response = await Client.PostAsJsonAsync("/api/contas", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Criar.FeatureResponse>();
        result.Should().NotBeNull();
        result!.Nome.Should().Be("Carteira Digital");
        result.Instituicao.Should().BeNull();
    }

    [Fact]
    public async Task Criar_ContaPoupanca_RetornaSucesso()
    {
        // Arrange
        var request = new Criar.FeatureRequest(
            Nome: "Poupança Caixa",
            Tipo: TipoConta.Poupanca,
            Instituicao: "Caixa Econômica Federal");

        // Act
        var response = await Client.PostAsJsonAsync("/api/contas", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Criar.FeatureResponse>();
        result.Should().NotBeNull();
        result!.Tipo.Should().Be(TipoConta.Poupanca);
    }

    [Fact]
    public async Task Criar_ContaInvestimento_RetornaSucesso()
    {
        // Arrange
        var request = new Criar.FeatureRequest(
            Nome: "XP Investimentos",
            Tipo: TipoConta.Investimento,
            Instituicao: "XP Inc.");

        // Act
        var response = await Client.PostAsJsonAsync("/api/contas", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Criar.FeatureResponse>();
        result.Should().NotBeNull();
        result!.Tipo.Should().Be(TipoConta.Investimento);
    }
}
