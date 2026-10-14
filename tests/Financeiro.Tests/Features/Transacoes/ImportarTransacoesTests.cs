using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Financeiro.Domain.Entities;
using Financeiro.Domain.Enums;
using Financeiro.Features.Transacoes;
using Financeiro.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Financeiro.Tests.Features.Transacoes;

public class ImportarTransacoesTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    // CSV idêntico ao exemplo real do Itaú (28 linhas)
    private const string CsvItau =
        "28/09/2026;RSCSS DRUGSTORE PO2809;-133,68\n" +
        "28/09/2026;REND PAGO APLIC AUT MAIS;0,01\n" +
        "29/09/2026;RSCSS 49 666 587 E2909;-12,00\n" +
        "29/09/2026;RSCSS SAVEGNAGO LO2909;-28,45\n" +
        "01/10/2026;RSHOP JUAREZ MATER0110;-42,60\n" +
        "01/10/2026;RSHOP MERCADO MORE0110;-4,49\n" +
        "05/10/2026;RSHOP MP COMERCIO0310;-10,00\n" +
        "05/10/2026;SISPAG PIX NETSYS SERVICOS E...;8079,00\n" +
        "05/10/2026;RSHOP 49 666 587 E0510;-12,00\n" +
        "06/10/2026;RSHOP SORVETERIA T0610;-41,00\n" +
        "06/10/2026;FATURA PAGA ITAU PLATINU;-4555,41\n" +
        "06/10/2026;REND PAGO APLIC AUT MAIS;0,01\n" +
        "07/10/2026;FINANC IMOBILIARIO 065/351;-1185,60\n" +
        "07/10/2026;REND PAGO APLIC AUT MAIS;0,01\n" +
        "08/10/2026;PAG BOLETO REALIZE SOCIEDADE DE CREDITO;-75,92\n" +
        "08/10/2026;PAG BOLETO PORTOSEG SA C FINANC E INVEST;-187,85\n" +
        "08/10/2026;PAGAMENTOCONTA VIVOFIXONAC;-87,40\n" +
        "08/10/2026;PIX QRS CPFL PAULIS08/10;-278,29\n" +
        "08/10/2026;PAG BOLETO SANDRA ANDRADE ADM COND LTDA;-458,61\n" +
        "08/10/2026;PAGAMENTO TRIBUTO DAS;-86,05\n" +
        "08/10/2026;PAGAMENTO TRIBUTO DAS;-86,05\n" +
        "08/10/2026;PIX TRANSF MARGARE08/10;-400,00\n" +
        "08/10/2026;PIX TRANSF DANIEL 08/10;-500,00\n" +
        "08/10/2026;DEV PIX DANIEL NIGR08/10;500,00\n" +
        "08/10/2026;PIX TRANSF DNL PSI08/10;-500,00\n" +
        "08/10/2026;RSHOP GOLD COPIAS 0810;-4,00\n" +
        "08/10/2026;REND PAGO APLIC AUT MAIS;0,01\n" +
        "13/10/2026;PAGAMENTOCONTA VIVO-SP;-39,64\n" +
        "13/10/2026;CAP     PIC        13/60;-61,29";

    private static MultipartFormDataContent CriarForm(Guid contaId, string csv)
    {
        var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        form.Add(fileContent, "arquivo", "extrato.csv");
        form.Add(new StringContent(contaId.ToString()), "contaId");
        return form;
    }

    [Fact]
    public async Task Importar_CsvValido_ImportaTodasAsLinhas()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Itaú", TipoConta.ContaCorrente, "Itaú Unibanco");
        context.Contas.Add(conta);
        await context.SaveChangesAsync();

        var form = CriarForm(conta.Id, CsvItau);

        // Act
        var response = await Client.PostAsync("/api/transacoes/importar", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Importar.FeatureResponse>();
        result.Should().NotBeNull();
        result!.Importadas.Should().Be(29);
        result.Erros.Should().BeEmpty();

        var contextAssert = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var transacoes = await contextAssert.Transacoes
            .Where(t => t.ContaId == conta.Id)
            .ToListAsync();

        transacoes.Should().HaveCount(29);
        transacoes.Should().AllSatisfy(t =>
        {
            t.Classificada.Should().BeFalse();
            t.DescricaoNormalizada.Should().NotBeNullOrWhiteSpace();
            t.Valor.Valor.Should().BeGreaterThan(0);
        });
    }

    [Fact]
    public async Task Importar_InferNaturezaPeloSinal()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Itaú", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);
        await context.SaveChangesAsync();

        var form = CriarForm(conta.Id, CsvItau);

        // Act
        await Client.PostAsync("/api/transacoes/importar", form);

        // Assert        
        var contextAssert = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();

        var despesas = await contextAssert.Transacoes
            .Where(t => t.ContaId == conta.Id && t.Natureza == NaturezaTransacao.Despesa)
            .CountAsync();
        var receitas = await contextAssert.Transacoes
            .Where(t => t.ContaId == conta.Id && t.Natureza == NaturezaTransacao.Receita)
            .CountAsync();

        despesas.Should().Be(23);
        receitas.Should().Be(6);
    }

    [Fact]
    public async Task Importar_NormalizaDescricao()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Itaú", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);
        await context.SaveChangesAsync();

        var form = CriarForm(conta.Id, "13/10/2026;CAP     PIC        13/60;-61,29");

        // Act
        await Client.PostAsync("/api/transacoes/importar", form);

        // Assert
        var contextAssert = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var transacao = await contextAssert.Transacoes
            .SingleAsync(t => t.ContaId == conta.Id);

        transacao.DescricaoOriginal.Should().Be("CAP     PIC        13/60");
        transacao.DescricaoNormalizada.Should().Be("CAP PIC 13/60");
    }

    [Fact]
    public async Task Importar_LinhasIdenticas_NaoDeduplica()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Itaú", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);
        await context.SaveChangesAsync();

        var form = CriarForm(conta.Id, CsvItau);

        // Act
        await Client.PostAsync("/api/transacoes/importar", form);

        // Assert
        var contextAssert = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var tributos = await contextAssert.Transacoes
            .Where(t => t.ContaId == conta.Id &&
                        t.DescricaoOriginal == "PAGAMENTO TRIBUTO DAS")
            .CountAsync();

        tributos.Should().Be(2);
    }

    [Fact]
    public async Task Importar_SemArquivo_RetornaBadRequest()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Itaú", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);
        await context.SaveChangesAsync();

        var form = new MultipartFormDataContent();
        form.Add(new StringContent(conta.Id.ToString()), "contaId");

        // Act
        var response = await Client.PostAsync("/api/transacoes/importar", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Importar_ContaIdInvalido_RetornaBadRequest()
    {
        // Arrange
        await ClearDatabaseAsync();

        var form = CriarForm(Guid.NewGuid(), CsvItau);
        form.Add(new StringContent("nao-e-guid"), "contaId");

        // Act
        var response = await Client.PostAsync("/api/transacoes/importar", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Importar_ContaInexistente_RetornaNotFound()
    {
        // Arrange
        await ClearDatabaseAsync();

        var form = CriarForm(Guid.NewGuid(), CsvItau);

        // Act
        var response = await Client.PostAsync("/api/transacoes/importar", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Importar_CsvMalformado_RetornaBadRequest()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Itaú", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);
        await context.SaveChangesAsync();

        var csvRuim = "28/09/2026;RSCSS DRUGSTORE";
        var form = CriarForm(conta.Id, csvRuim);

        // Act
        var response = await Client.PostAsync("/api/transacoes/importar", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Importar_CsvMalformado_NaoPersisteNada()
    {
        // Arrange
        await ClearDatabaseAsync();

        await using var scope = CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var conta = new Conta("Itaú", TipoConta.ContaCorrente, null);
        context.Contas.Add(conta);
        await context.SaveChangesAsync();

        // 1 linha boa + 1 linha ruim → atômico, nada persiste
        var csvMisto =
            "28/09/2026;REND PAGO APLIC AUT MAIS;0,01\n" +
            "28/09/2026;RSCSS DRUGSTORE";
        var form = CriarForm(conta.Id, csvMisto);

        // Act
        var response = await Client.PostAsync("/api/transacoes/importar", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var contextAssert = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var total = await contextAssert.Transacoes.CountAsync();
        total.Should().Be(0);
    }
}