using System.Net;
using System.Text;
using Bunit;
using Financeiro.Api.Components.Pages;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Financeiro.Tests.Components;

public sealed class RelatoriosBlazorTests
{
    [Fact]
    public void Transacoes_RenderizaTituloETabela()
    {
        using var contexto = CriarContexto();

        var corte = contexto.RenderComponent<Transacoes>();

        corte.WaitForAssertion(() =>
            corte.Markup.Should().Contain("Lista analítica"));
    }

    [Fact]
    public void RelatoriosPorConta_RenderizaTituloEResumo()
    {
        using var contexto = CriarContexto("""{"linhas":[],"total":{"contaId":"00000000-0000-0000-0000-000000000000","contaNome":"TOTAL","totalReceitas":100,"totalDespesas":40,"totalTransferencias":0,"totalInvestimentos":0,"saldoPeriodo":60}}""");

        var corte = contexto.RenderComponent<RelatoriosPorConta>();

        corte.WaitForAssertion(() =>
            corte.Markup.Should().Contain("Por conta"));
    }

    [Fact]
    public void RelatoriosComposicao_RenderizaTitulo()
    {
        using var contexto = CriarContexto("""{"receitas":[],"despesas":[]}""");

        var corte = contexto.RenderComponent<RelatoriosComposicao>();

        corte.WaitForAssertion(() =>
            corte.Markup.Should().Contain("Composição"));
    }

    [Fact]
    public void RelatoriosFluxoCaixa_RenderizaTitulo()
    {
        using var contexto = CriarContexto("""{"dataInicio":"2026-11-01","dataFim":"2026-11-17","saldoInicial":0,"totalEntradas":0,"totalSaidas":0,"saldoFinal":0,"diario":[]}""");

        var corte = contexto.RenderComponent<RelatoriosFluxoCaixa>();

        corte.WaitForAssertion(() =>
            corte.Markup.Should().Contain("Fluxo realizado"));
    }

    [Fact]
    public void RelatoriosComparacao_RenderizaTitulo()
    {
        using var contexto = CriarContexto("""{"periodoA":{"inicio":"2026-10-01","fim":"2026-10-31","totalReceitas":0,"totalDespesas":0,"saldo":0},"periodoB":{"inicio":"2026-11-01","fim":"2026-11-17","totalReceitas":0,"totalDespesas":0,"saldo":0},"receitas":[],"despesas":[]}""");

        var corte = contexto.RenderComponent<RelatoriosComparacao>();

        corte.WaitForAssertion(() =>
            corte.Markup.Should().Contain("Comparação"));
    }

    [Fact]
    public void NovaTransacao_RenderizaFormulario()
    {
        using var contexto = CriarContexto();

        var corte = contexto.RenderComponent<NovaTransacao>();

        corte.WaitForAssertion(() =>
            corte.Markup.Should().Contain("Nova transação"));
    }

    [Fact]
    public void RevisaoTransacoes_RenderizaFila()
    {
        using var contexto = CriarContexto();

        var corte = contexto.RenderComponent<RevisaoTransacoes>();

        corte.WaitForAssertion(() =>
            corte.Markup.Should().Contain("Nenhuma transação aguarda classificação"));
    }

    private static TestContext CriarContexto(string json)
    {
        var contexto = new TestContext();
        contexto.JSInterop.Mode = JSRuntimeMode.Loose;
        contexto.Services.AddMudServices();
        contexto.Services.AddSingleton<IHttpClientFactory>(
            new FakeHttpClientFactory(json));
        return contexto;
    }

    private static TestContext CriarContexto()
    {
        var contexto = new TestContext();
        contexto.JSInterop.Mode = JSRuntimeMode.Loose;
        contexto.Services.AddMudServices();
        contexto.Services.AddSingleton<IHttpClientFactory>(
            new RouteFakeHttpClientFactory());
        contexto.RenderComponent<MudPopoverProvider>();
        return contexto;
    }

    private sealed class FakeHttpClientFactory(string json) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(new FakeHandler(json));
    }

    private sealed class FakeHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
    }

    private sealed class RouteFakeHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(new RouteFakeHandler());
    }

    private sealed class RouteFakeHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var json = request.RequestUri?.AbsolutePath switch
            {
                "/api/categorias" => "[]",
                "/api/tags" => "[]",
                "/api/transacoes" => "[]",
                _ => "[]"
            };

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }
}
