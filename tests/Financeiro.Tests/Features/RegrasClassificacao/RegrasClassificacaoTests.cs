using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Financeiro.Domain.Entities;
using Financeiro.Domain.Enums;
using Financeiro.Domain.ValueObjects;
using Financeiro.Features.RegrasClassificacao;
using Financeiro.Features.Transacoes;
using Financeiro.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using CriarRegra = Financeiro.Features.RegrasClassificacao.Criar;
using CriarTransacao = Financeiro.Features.Transacoes.Criar;

namespace Financeiro.Tests.Features.RegrasClassificacao;

public class RegrasClassificacaoTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Criar_RegraCorrespondente_ClassificaTransacaoManual()
    {
        // Arrange
        await ClearDatabaseAsync();
        var (conta, categoria) = await PrepararContaECategoriaAsync();
        await CriarRegraAsync(categoria.Id, "mercado central", prioridade: 5);

        var request = new CriarTransacao.FeatureRequest(
            new DateOnly(2026, 11, 21),
            "Compra no Mercado Central",
            32.50m,
            NaturezaTransacao.Despesa,
            conta.Id);

        // Act
        var response = await Client.PostAsJsonAsync("/api/transacoes", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TransacaoResponse>();
        result.Should().NotBeNull();
        result!.Classificada.Should().BeTrue();
        result.CategoriaId.Should().Be(categoria.Id);
    }

    [Fact]
    public async Task Importar_RegraCorrespondente_ClassificaTransacaoImportada()
    {
        // Arrange
        await ClearDatabaseAsync();
        var (conta, categoria) = await PrepararContaECategoriaAsync();
        await CriarRegraAsync(categoria.Id, "mercado central", prioridade: 5);

        using var form = new MultipartFormDataContent();
        var arquivo = new ByteArrayContent(Encoding.UTF8.GetBytes(
            "21/11/2026;COMPRA MERCADO CENTRAL;-32,50"));
        arquivo.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        form.Add(arquivo, "arquivo", "extrato.csv");
        form.Add(new StringContent(conta.Id.ToString()), "contaId");

        // Act
        var response = await Client.PostAsync("/api/transacoes/importar", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await using var verifyScope = CreateAsyncScope();
        var verify = verifyScope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var transacao = await verify.Transacoes.SingleAsync();
        transacao.Classificada.Should().BeTrue();
        transacao.CategoriaId.Should().Be(categoria.Id);
    }

    [Fact]
    public async Task Criar_RegrasConflitantes_UsaMaiorPrioridade()
    {
        // Arrange
        await ClearDatabaseAsync();
        var (conta, categoria) = await PrepararContaECategoriaAsync();
        var categoriaPrioritaria = await CriarCategoriaAsync("Mercado específico");
        await CriarRegraAsync(categoria.Id, "mercado", prioridade: 1);
        await CriarRegraAsync(categoriaPrioritaria.Id, "mercado central", prioridade: 10);

        var request = new CriarTransacao.FeatureRequest(
            new DateOnly(2026, 11, 21),
            "Compra no Mercado Central",
            32.50m,
            NaturezaTransacao.Despesa,
            conta.Id);

        // Act
        var response = await Client.PostAsJsonAsync("/api/transacoes", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TransacaoResponse>();
        result.Should().NotBeNull();
        result!.Classificada.Should().BeTrue();
        result.CategoriaId.Should().Be(categoriaPrioritaria.Id);
    }

    [Fact]
    public async Task Criar_SemRegraCorrespondente_PermanecePendente()
    {
        // Arrange
        await ClearDatabaseAsync();
        var (conta, categoria) = await PrepararContaECategoriaAsync();
        await CriarRegraAsync(categoria.Id, "mercado", prioridade: 1);

        var request = new CriarTransacao.FeatureRequest(
            new DateOnly(2026, 11, 21),
            "Farmácia",
            32.50m,
            NaturezaTransacao.Despesa,
            conta.Id);

        // Act
        var response = await Client.PostAsJsonAsync("/api/transacoes", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TransacaoResponse>();
        result.Should().NotBeNull();
        result!.Classificada.Should().BeFalse();

        var pendentesResponse = await Client.GetAsync("/api/transacoes/pendentes");
        var pendentes = await pendentesResponse.Content.ReadFromJsonAsync<TransacaoResponse[]>();
        pendentes.Should().ContainSingle(t => t.Id == result.Id);
    }

    [Fact]
    public async Task Sugerir_RegraAtivaEHistoricoDisponiveis_PriorizaRegra()
    {
        // Arrange
        await ClearDatabaseAsync();
        var (conta, categoriaHistorico) = await PrepararContaECategoriaAsync();
        var categoriaRegra = await CriarCategoriaAsync("Classificação da regra");
        await CriarRegraAsync(categoriaRegra.Id, "academia", prioridade: 10);

        await using (var scope = CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
            var contaPersistida = await context.Contas.SingleAsync(c => c.Id == conta.Id);
            var categoriaHistoricoPersistida =
                await context.Categorias.SingleAsync(c => c.Id == categoriaHistorico.Id);
            var historico = new Transacao(
                new DateOnly(2026, 11, 1),
                "Academia plano mensal",
                new Dinheiro(100m, "BRL"),
                NaturezaTransacao.Despesa,
                contaPersistida);
            historico.Classificar(categoriaHistoricoPersistida, Contexto.Familiar, Pessoa.Conjuge);
            context.Transacoes.Add(historico);
            await context.SaveChangesAsync();
        }

        var transacaoPendente = await CriarTransacaoPendenteAsync(
            conta.Id,
            "Academia mensal",
            NaturezaTransacao.Despesa);

        // Act
        var response = await Client.GetAsync(
            $"/api/transacoes/{transacaoPendente.Id}/sugestao-classificacao");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SugerirClassificacao.FeatureResponse>();
        result.Should().NotBeNull();
        result!.TemSugestao.Should().BeTrue();
        result.Origem.Should().Be("RegraAutomatica");
        result.CategoriaId.Should().Be(categoriaRegra.Id);
        result.RegraId.Should().NotBeNull();
    }

    [Fact]
    public async Task Sugerir_SemRegra_UsaNaturezaEDescricaoNormalizadaDoHistorico()
    {
        // Arrange
        await ClearDatabaseAsync();
        var (conta, categoriaDespesa) = await PrepararContaECategoriaAsync();
        var categoriaReceita = await CriarCategoriaAsync("Receita");

        await using (var scope = CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
            var contaPersistida = await context.Contas.SingleAsync(c => c.Id == conta.Id);
            var categoriaDespesaPersistida =
                await context.Categorias.SingleAsync(c => c.Id == categoriaDespesa.Id);
            var categoriaReceitaPersistida =
                await context.Categorias.SingleAsync(c => c.Id == categoriaReceita.Id);
            var historicoDespesa = new Transacao(
                new DateOnly(2026, 11, 1),
                "Café São João 123456",
                new Dinheiro(20m, "BRL"),
                NaturezaTransacao.Despesa,
                contaPersistida);
            historicoDespesa.Classificar(categoriaDespesaPersistida, Contexto.Pessoal, Pessoa.Eu);

            // A descrição é igual, mas uma natureza diferente não deve contaminar a sugestão.
            var historicoReceita = new Transacao(
                new DateOnly(2026, 11, 2),
                "Cafe Sao Joao 987654",
                new Dinheiro(200m, "BRL"),
                NaturezaTransacao.Receita,
                contaPersistida);
            historicoReceita.Classificar(categoriaReceitaPersistida, Contexto.YouTube, Pessoa.Eu);

            context.Transacoes.AddRange(historicoDespesa, historicoReceita);
            await context.SaveChangesAsync();
        }

        var transacaoPendente = await CriarTransacaoPendenteAsync(
            conta.Id,
            "CAFE SAO JOAO 654321",
            NaturezaTransacao.Despesa);

        // Act
        var response = await Client.GetAsync(
            $"/api/transacoes/{transacaoPendente.Id}/sugestao-classificacao");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SugerirClassificacao.FeatureResponse>();
        result.Should().NotBeNull();
        result!.TemSugestao.Should().BeTrue();
        result.Origem.Should().Be("Historico");
        result.CategoriaId.Should().Be(categoriaDespesa.Id);
        result.CategoriaId.Should().NotBe(categoriaReceita.Id);
        result.RegraId.Should().BeNull();
    }

    [Theory]
    [InlineData(NaturezaTransacao.Transferencia)]
    [InlineData(NaturezaTransacao.Investimento)]
    public async Task Criar_NaturezaNeutraComRegraCorrespondente_NaoClassificaNemSugere(
        NaturezaTransacao natureza)
    {
        // Arrange
        await ClearDatabaseAsync();
        var (conta, categoria) = await PrepararContaECategoriaAsync();
        await CriarRegraAsync(categoria.Id, "aporte xp", prioridade: 10);

        var request = new CriarTransacao.FeatureRequest(
            new DateOnly(2026, 11, 21),
            "Aporte XP mensal",
            100m,
            natureza,
            conta.Id);

        // Act
        var response = await Client.PostAsJsonAsync("/api/transacoes", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var criada = await response.Content.ReadFromJsonAsync<TransacaoResponse>();
        criada.Should().NotBeNull();
        criada!.Classificada.Should().BeFalse();

        var sugestaoResponse = await Client.GetAsync(
            $"/api/transacoes/{criada.Id}/sugestao-classificacao");
        sugestaoResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var sugestao = await sugestaoResponse.Content
            .ReadFromJsonAsync<SugerirClassificacao.FeatureResponse>();
        sugestao.Should().NotBeNull();
        sugestao!.TemSugestao.Should().BeFalse();
    }

    [Fact]
    public async Task Desativar_RegraAtiva_NaoAplicaEmNovasTransacoes()
    {
        // Arrange
        await ClearDatabaseAsync();
        var (conta, categoria) = await PrepararContaECategoriaAsync();
        var regra = await CriarRegraAsync(categoria.Id, "mercado", prioridade: 1);

        // Act
        var response = await Client.PutAsync(
            $"/api/regras-classificacao/{regra.Id}/desativar",
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var desativada = await response.Content.ReadFromJsonAsync<RegraClassificacaoResponse>();
        desativada.Should().NotBeNull();
        desativada!.Ativa.Should().BeFalse();

        var transacaoResponse = await Client.PostAsJsonAsync(
            "/api/transacoes",
            new CriarTransacao.FeatureRequest(
                new DateOnly(2026, 11, 21),
                "Compra no mercado",
                40m,
                NaturezaTransacao.Despesa,
                conta.Id));
        transacaoResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var transacao = await transacaoResponse.Content.ReadFromJsonAsync<TransacaoResponse>();
        transacao.Should().NotBeNull();
        transacao!.Classificada.Should().BeFalse();
    }

    private async Task<(Conta Conta, Categoria Categoria)> PrepararContaECategoriaAsync()
    {
        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Conta de teste", TipoConta.ContaCorrente, null);
        var categoria = new Categoria("Categoria de teste");
        context.Contas.Add(conta);
        context.Categorias.Add(categoria);
        await context.SaveChangesAsync();
        return (conta, categoria);
    }

    private async Task<Categoria> CriarCategoriaAsync(string nome)
    {
        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var categoria = new Categoria(nome);
        context.Categorias.Add(categoria);
        await context.SaveChangesAsync();
        return categoria;
    }

    private async Task<RegraClassificacaoResponse> CriarRegraAsync(
        Guid categoriaId,
        string descricao,
        int prioridade)
    {
        var response = await Client.PostAsJsonAsync(
            "/api/regras-classificacao",
            new CriarRegra.FeatureRequest(
                descricao,
                categoriaId,
                Contexto.Pessoal,
                Pessoa.Eu,
                prioridade));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var regra = await response.Content.ReadFromJsonAsync<RegraClassificacaoResponse>();
        regra.Should().NotBeNull();
        return regra!;
    }

    private async Task<Transacao> CriarTransacaoPendenteAsync(
        Guid contaId,
        string descricao,
        NaturezaTransacao natureza)
    {
        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = await context.Contas.SingleAsync(c => c.Id == contaId);
        var transacao = new Transacao(
            new DateOnly(2026, 11, 21),
            descricao,
            new Dinheiro(25m, "BRL"),
            natureza,
            conta);
        context.Transacoes.Add(transacao);
        await context.SaveChangesAsync();
        return transacao;
    }
}
