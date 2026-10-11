using Financeiro.Infrastructure.Importers;
using FluentAssertions;
using Xunit;

namespace Financeiro.Tests.Infrastructure.Importers;

public class DescricaoNormalizadorTests
{
    [Fact]
    public void Normalizar_SemAlteracoes_RetornaMesmoTexto()
    {
        var resultado = DescricaoNormalizador.Normalizar("RSCSS DRUGSTORE PO2809");

        resultado.Should().Be("RSCSS DRUGSTORE PO2809");
    }

    [Fact]
    public void Normalizar_ComEspacosMultiplos_Colapsa()
    {
        var resultado = DescricaoNormalizador.Normalizar("CAP     PIC        13/60");

        resultado.Should().Be("CAP PIC 13/60");
    }

    [Fact]
    public void Normalizar_ComAcentos_RemoveAcentos()
    {
        var resultado = DescricaoNormalizador.Normalizar("Padaria São João");

        resultado.Should().Be("PADARIA SAO JOAO");
    }

    [Fact]
    public void Normalizar_Minusculas_ConverteParaMaiusculas()
    {
        var resultado = DescricaoNormalizador.Normalizar("pix enviado");

        resultado.Should().Be("PIX ENVIADO");
    }

    [Fact]
    public void Normalizar_ComNumeroFinalLongo_RemoveNumero()
    {
        var resultado = DescricaoNormalizador.Normalizar("PIX ENVIADO 00012345");

        resultado.Should().Be("PIX ENVIADO");
    }

    [Fact]
    public void Normalizar_ComNumeroCurtoNoFinal_NaoRemove()
    {
        var resultado = DescricaoNormalizador.Normalizar("LOJA 2024");

        resultado.Should().Be("LOJA 2024");
    }

    [Fact]
    public void Normalizar_ComBarraNoFinal_NaoRemove()
    {
        var resultado = DescricaoNormalizador.Normalizar("PIX QRS CPFL PAULIS08/10");

        resultado.Should().Be("PIX QRS CPFL PAULIS08/10");
    }

    [Fact]
    public void Normalizar_DescricaoVazia_LancaArgumentException()
    {
        var act = () => DescricaoNormalizador.Normalizar("  ");

        act.Should().Throw<ArgumentException>();
    }
}