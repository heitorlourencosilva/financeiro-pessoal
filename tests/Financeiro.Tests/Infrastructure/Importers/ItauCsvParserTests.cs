using System.Text;
using Financeiro.Domain.Enums;
using Financeiro.Infrastructure.Importers;
using FluentAssertions;
using Xunit;

namespace Financeiro.Tests.Infrastructure.Importers;

public class ItauCsvParserTests
{
    private static ItauCsvParser CriarParser() => new();

    private static TextReader Reader(string conteudo) =>
        new StringReader(conteudo);

    [Fact]
    public void Parse_LinhaDespesa_RetornaValorAbsolutoENaturezaDespesa()
    {
        var csv = "28/09/2026;RSCSS DRUGSTORE PO2809;-133,68";

        var resultado = CriarParser().Parse(Reader(csv));

        resultado.Should().HaveCount(1);
        resultado[0].Data.Should().Be(new DateOnly(2026, 9, 28));
        resultado[0].DescricaoOriginal.Should().Be("RSCSS DRUGSTORE PO2809");
        resultado[0].Valor.Valor.Should().Be(133.68m);
        resultado[0].Valor.Moeda.Should().Be("BRL");
        resultado[0].Natureza.Should().Be(NaturezaTransacao.Despesa);
    }

    [Fact]
    public void Parse_LinhaReceita_RetornaNaturezaReceita()
    {
        var csv = "05/10/2026;SISPAG PIX NETSYS SERVICOS E...;8079,00";

        var resultado = CriarParser().Parse(Reader(csv));

        resultado.Should().HaveCount(1);
        resultado[0].Valor.Valor.Should().Be(8079.00m);
        resultado[0].Natureza.Should().Be(NaturezaTransacao.Receita);
    }

    [Fact]
    public void Parse_ValorComCentavos_LeCorretamente()
    {
        var csv = "28/09/2026;REND PAGO APLIC AUT MAIS;0,01";

        var resultado = CriarParser().Parse(Reader(csv));

        resultado[0].Valor.Valor.Should().Be(0.01m);
        resultado[0].Natureza.Should().Be(NaturezaTransacao.Receita);
    }

    [Fact]
    public void Parse_LinhasVazias_SaoIgnoradas()
    {
        var csv = "\n28/09/2026;REND PAGO APLIC AUT MAIS;0,01\n\n";

        var resultado = CriarParser().Parse(Reader(csv));

        resultado.Should().HaveCount(1);
    }

    [Fact]
    public void Parse_LinhasIdenticas_NaoDeduplica()
    {
        var csv = "08/10/2026;PAGAMENTO TRIBUTO DAS;-86,05\n" +
                  "08/10/2026;PAGAMENTO TRIBUTO DAS;-86,05";

        var resultado = CriarParser().Parse(Reader(csv));

        resultado.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_NumeroDeColunasErrado_LancaFormatException()
    {
        var csv = "28/09/2026;RSCSS DRUGSTORE";

        var act = () => CriarParser().Parse(Reader(csv));

        act.Should().Throw<FormatException>()
            .WithMessage("*Linha 1*");
    }

    [Fact]
    public void Parse_DataInvalida_LancaFormatException()
    {
        var csv = "2026-09-28;RSCSS DRUGSTORE;-133,68";

        var act = () => CriarParser().Parse(Reader(csv));

        act.Should().Throw<FormatException>()
            .WithMessage("*data inválida*");
    }

    [Fact]
    public void Parse_ValorInvalido_LancaFormatException()
    {
        var csv = "28/09/2026;RSCSS DRUGSTORE;abc";

        var act = () => CriarParser().Parse(Reader(csv));

        act.Should().Throw<FormatException>()
            .WithMessage("*valor inválido*");
    }

    [Fact]
    public void Parse_DescricaoVazia_LancaFormatException()
    {
        var csv = "28/09/2026;;-133,68";

        var act = () => CriarParser().Parse(Reader(csv));

        act.Should().Throw<FormatException>()
            .WithMessage("*descrição vazia*");
    }
}