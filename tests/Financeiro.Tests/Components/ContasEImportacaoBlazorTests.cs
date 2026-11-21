using System.Net;
using System.Text;
using Bunit;
using Financeiro.Api.Components.Pages;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Financeiro.Tests.Components;

public sealed class ContasBlazorTests
{
    private static readonly Guid ContaId = Guid.Parse("a1111111-1111-1111-1111-111111111111");

    [Fact]
    public void Contas_RenderizaFormularioEListaDeContas()
    {
        using var contexto = CriarContexto(_ => Resposta(HttpStatusCode.OK, ContasJson()));

        var corte = contexto.RenderComponent<Contas>();

        corte.WaitForAssertion(() =>
        {
            corte.Markup.Should().Contain("Criar conta");
            corte.Markup.Should().Contain("Nome da conta");
            corte.Markup.Should().Contain("Tipo de conta");
            corte.Markup.Should().Contain("Instituição (opcional)");
            corte.Markup.Should().Contain("Conta teste");
            corte.Markup.Should().Contain("Banco Itaú");
        });
        contexto.Services.GetRequiredService<RecordingHttpMessageHandler>()
            .Requests.Should().ContainSingle(request =>
                request.Method == HttpMethod.Get && request.Path == "/api/contas");
    }

    [Fact]
    public void Contas_CriarSemNomeMostraValidacaoSemChamarEndpoint()
    {
        using var contexto = CriarContexto(_ => Resposta(HttpStatusCode.OK, "[]"));
        var corte = contexto.RenderComponent<Contas>();

        Botao(corte, "Criar conta").Click();

        corte.WaitForAssertion(() =>
            corte.Find("[role=alert]").TextContent.Should().Contain("Informe o nome da conta."));
        contexto.Services.GetRequiredService<RecordingHttpMessageHandler>()
            .Requests.Should().ContainSingle(request => request.Method == HttpMethod.Get);
    }

    [Fact]
    public void Contas_CriarEnviaCamposCorretosEApresentaSucesso()
    {
        using var contexto = CriarContexto(request =>
            Resposta(HttpStatusCode.OK, request.Method == HttpMethod.Get ? ContasJson() : "{}"));
        var corte = contexto.RenderComponent<Contas>();

        corte.FindAll("input[type=text]")[0].Input("Conta nova");
        corte.FindAll("input[type=text]")[1].Change("Banco novo");
        Botao(corte, "Criar conta").Click();

        var handler = contexto.Services.GetRequiredService<RecordingHttpMessageHandler>();
        handler.Requests.Should().Contain(request =>
            request.Method == HttpMethod.Post &&
            request.Path == "/api/contas" &&
            request.Body.Contains("\"nome\":\"Conta nova\"") &&
            request.Body.Contains("\"tipo\":1") &&
            request.Body.Contains("\"instituicao\":\"Banco novo\""));
        corte.WaitForAssertion(() => corte.Find("input[type=text]").GetAttribute("value").Should().BeEmpty());
    }

    [Fact]
    public void Contas_FalhaAoCriarExibeDetalheDaApi()
    {
        using var contexto = CriarContexto(request => request.Method == HttpMethod.Post
            ? Resposta(HttpStatusCode.BadRequest, """{"detail":"Nome duplicado"}""")
            : Resposta(HttpStatusCode.OK, "[]"));
        var corte = contexto.RenderComponent<Contas>();
        corte.Find("input[type=text]").Input("Conta existente");

        Botao(corte, "Criar conta").Click();

        corte.WaitForAssertion(() =>
            corte.Find("[role=alert]").TextContent.Should().Contain("Nome duplicado"));
        contexto.Services.GetRequiredService<RecordingHttpMessageHandler>()
            .Requests.Should().Contain(request => request.Method == HttpMethod.Post && request.Path == "/api/contas");
    }

    [Fact]
    public void Contas_RenomearEnviaNomeParaEndpointDaConta()
    {
        using var contexto = CriarContexto(_ => Resposta(HttpStatusCode.OK, ContasJson()));
        var corte = contexto.RenderComponent<Contas>();

        Botao(corte, "Renomear").Click();
        corte.FindAll("input[type=text]").Last().Input("Conta renomeada");
        Botao(corte, "Salvar").Click();

        contexto.Services.GetRequiredService<RecordingHttpMessageHandler>()
            .Requests.Should().Contain(request =>
                request.Method == HttpMethod.Put &&
                request.Path == $"/api/contas/{ContaId}" &&
                request.Body.Contains("\"nome\":\"Conta renomeada\""));
        corte.WaitForAssertion(() => corte.FindAll("input[type=text]").Last().GetAttribute("value").Should().BeNull());
    }

    [Fact]
    public void Contas_DesativarChamaEndpointDaConta()
    {
        using var contexto = CriarContexto(_ => Resposta(HttpStatusCode.OK, ContasJson()));
        var corte = contexto.RenderComponent<Contas>();

        Botao(corte, "Desativar").Click();

        contexto.Services.GetRequiredService<RecordingHttpMessageHandler>()
            .Requests.Should().Contain(request =>
                request.Method == HttpMethod.Delete &&
                request.Path == $"/api/contas/{ContaId}/desativar");
    }

    private static TestContext CriarContexto(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var contexto = new TestContext();
        contexto.JSInterop.Mode = JSRuntimeMode.Loose;
        contexto.Services.AddMudServices();
        contexto.Services.AddSingleton(new RecordingHttpMessageHandler(responder));
        contexto.Services.AddSingleton<IHttpClientFactory, RecordingHttpClientFactory>();
        contexto.RenderComponent<MudPopoverProvider>();
        contexto.RenderComponent<MudSnackbarProvider>();
        return contexto;
    }

    private static AngleSharp.Dom.IElement Botao(IRenderedFragment corte, string texto) =>
        corte.FindAll("button").Single(button => button.TextContent.Contains(texto, StringComparison.Ordinal));

    private static string ContasJson() =>
        $$"""[{"id":"{{ContaId}}","nome":"Conta teste","tipo":1,"instituicao":"Banco Itaú","ativa":true,"criadaEm":"2026-01-01T00:00:00Z"}]""";

    private static HttpResponseMessage Resposta(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}

public sealed class ImportarTransacoesBlazorTests
{
    private static readonly Guid ContaId = Guid.Parse("b2222222-2222-2222-2222-222222222222");
    private const string ContasJson =
        """[{"id":"b2222222-2222-2222-2222-222222222222","nome":"Conta ativa","ativa":true},{"id":"c3333333-3333-3333-3333-333333333333","nome":"Conta inativa","ativa":false}]""";

    [Fact]
    public void ImportarTransacoes_RenderizaFormularioEListaSomenteContasAtivas()
    {
        using var contexto = CriarContexto(_ => Resposta(HttpStatusCode.OK, ContasJson));

        var corte = contexto.RenderComponent<ImportarTransacoes>();

        corte.WaitForAssertion(() =>
        {
            corte.Markup.Should().Contain("Importar CSV Itaú");
            corte.Markup.Should().Contain("Conta ativa");
            corte.Markup.Should().Contain("Arquivo CSV");
            corte.Markup.Should().Contain("Conta ativa");
            corte.Markup.Should().NotContain("Conta inativa");
            corte.Find("#csv-file").GetAttribute("accept").Should().Contain(".csv");
        });
        contexto.Services.GetRequiredService<RecordingHttpMessageHandler>()
            .Requests.Should().ContainSingle(request => request.Method == HttpMethod.Get && request.Path == "/api/contas");
    }

    [Fact]
    public void ImportarTransacoes_SemArquivoMostraValidacaoSemEnviar()
    {
        using var contexto = CriarContexto(_ => Resposta(HttpStatusCode.OK, ContasJson));
        var corte = contexto.RenderComponent<ImportarTransacoes>();

        Botao(corte, "Importar arquivo").Click();

        corte.WaitForAssertion(() =>
            corte.Find("[role=alert]").TextContent.Should().Contain("Selecione um arquivo CSV."));
        contexto.Services.GetRequiredService<RecordingHttpMessageHandler>()
            .Requests.Should().NotContain(request => request.Method == HttpMethod.Post);
    }

    [Fact]
    public void ImportarTransacoes_EnviaMultipartComArquivoEContaEApresentaSucesso()
    {
        using var contexto = CriarContexto(_ => Resposta(
            HttpStatusCode.OK,
            """{"importadas":2,"erros":[]}"""));
        var corte = contexto.RenderComponent<ImportarTransacoes>();
        corte.FindComponent<InputFile>().UploadFiles(
            InputFileContent.CreateFromText("csv-content", "movimentacoes.csv", contentType: "text/csv"));

        Botao(corte, "Importar arquivo").Click();

        var handler = contexto.Services.GetRequiredService<RecordingHttpMessageHandler>();
        handler.Requests.Should().Contain(request =>
            request.Method == HttpMethod.Post &&
            request.Path == "/api/transacoes/importar" &&
            request.ContentType!.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase) &&
            request.Body.Contains("name=arquivo; filename=movimentacoes.csv") &&
            request.Body.Contains("csv-content") &&
            request.Body.Contains("name=contaId") &&
            request.Body.Contains(ContaId.ToString()));
        corte.WaitForAssertion(() =>
            corte.Find("[role=status]").TextContent.Should().Contain("2 movimentação(ões) importada(s) com sucesso."));
    }

    [Fact]
    public void ImportarTransacoes_RespostaHttpComErroMostraDetalheEPreservaFalha()
    {
        using var contexto = CriarContexto(_ => Resposta(
            HttpStatusCode.BadRequest,
            """{"detail":"Arquivo CSV inválido"}"""));
        var corte = contexto.RenderComponent<ImportarTransacoes>();
        corte.FindComponent<InputFile>().UploadFiles(
            InputFileContent.CreateFromText("conteudo", "invalido.csv", contentType: "text/csv"));

        Botao(corte, "Importar arquivo").Click();

        corte.WaitForAssertion(() =>
            corte.Find("[role=alert]").TextContent.Should().Contain("Arquivo CSV inválido"));
        contexto.Services.GetRequiredService<RecordingHttpMessageHandler>()
            .Requests.Should().Contain(request => request.Method == HttpMethod.Post && request.Path == "/api/transacoes/importar");
        corte.Markup.Should().NotContain("importada(s) com sucesso");
    }

    private static TestContext CriarContexto(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var contexto = new TestContext();
        contexto.JSInterop.Mode = JSRuntimeMode.Loose;
        contexto.Services.AddMudServices();
        contexto.Services.AddSingleton(new RecordingHttpMessageHandler(request =>
            request.RequestUri?.AbsolutePath == "/api/contas"
                ? Resposta(HttpStatusCode.OK, ContasJson)
                : responder(request)));
        contexto.Services.AddSingleton<IHttpClientFactory, RecordingHttpClientFactory>();
        contexto.RenderComponent<MudPopoverProvider>();
        contexto.RenderComponent<MudSnackbarProvider>();
        return contexto;
    }

    private static AngleSharp.Dom.IElement Botao(IRenderedFragment corte, string texto) =>
        corte.FindAll("button").Single(button => button.TextContent.Contains(texto, StringComparison.Ordinal));

    private static HttpResponseMessage Resposta(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}

internal sealed class RecordingHttpClientFactory(RecordingHttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
}

internal sealed class RecordingHttpMessageHandler(
    Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
    public List<RecordedRequest> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(new RecordedRequest(
            request.Method,
            request.RequestUri!.AbsolutePath,
            request.Content?.Headers.ContentType?.ToString(),
            request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken)));
        return responder(request);
    }
}

internal sealed record RecordedRequest(HttpMethod Method, string Path, string? ContentType, string Body);
