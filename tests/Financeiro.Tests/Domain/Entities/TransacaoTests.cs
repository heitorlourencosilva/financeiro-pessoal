using FluentAssertions;
using Financeiro.Domain.Entities;
using Financeiro.Domain.Enums;
using Financeiro.Domain.ValueObjects;
using Xunit;
using Financeiro.Domain.Services;

namespace Financeiro.Tests.Domain.Entities;

public class TransacaoTests
{
    private static Conta CriarConta() => new("Conta Teste", TipoConta.ContaCorrente);

    private static Transacao CriarTransacao(
        NaturezaTransacao natureza,
        decimal valor = 100m,
        Contexto contexto = Contexto.Pessoal,
        Pessoa pessoa = Pessoa.Eu)
    {
        return new Transacao(
            data: new DateOnly(2026, 10, 10),
            descricaoOriginal: "Teste transaction",
            valor: new Dinheiro(valor, "BRL"),
            natureza: natureza,
            conta: CriarConta(),
            contexto: contexto,
            pessoa: pessoa);
    }

    // ---------- D-001: Transferência é neutra ----------

    [Fact]
    public void Transferencia_EhNeutra_NaoAfetaFluxoDeCaixa()
    {
        var t = CriarTransacao(NaturezaTransacao.Transferencia);

        t.EhNeutra().Should().BeTrue();
        t.AfetaFluxoDeCaixa().Should().BeFalse();
        t.SinalFluxo().Should().Be(0);
    }

    [Fact]
    public void Receita_NaoEhNeutra_ContribuiParaReceita()
    {
        var t = CriarTransacao(NaturezaTransacao.Receita);

        t.EhNeutra().Should().BeFalse();
        t.AfetaFluxoDeCaixa().Should().BeTrue();
        t.SinalFluxo().Should().Be(1);
    }

    [Fact]
    public void Despesa_NaoEhNeutra_ContribuiParaDespesa()
    {
        var t = CriarTransacao(NaturezaTransacao.Despesa);

        t.EhNeutra().Should().BeFalse();
        t.AfetaFluxoDeCaixa().Should().BeTrue();
        t.SinalFluxo().Should().Be(-1);
    }

    // ---------- D-003: Aporte/Resgate é transferência neutra ----------

    [Fact]
    public void Investimento_Aporte_EhNeutra_NaoAfetaFluxoDeCaixa()
    {
        var t = CriarTransacao(NaturezaTransacao.Investimento);

        t.EhNeutra().Should().BeTrue();
        t.AfetaFluxoDeCaixa().Should().BeFalse();
        t.SinalFluxo().Should().Be(0);
    }

    // ---------- D-004: Dinheiro com decimal ----------

    [Fact]
    public void Transacao_UsaDinheiroComoValor()
    {
        var t = CriarTransacao(NaturezaTransacao.Despesa, 123.45m);

        t.Valor.Should().Be(new Dinheiro(123.45m, "BRL"));
    }

    [Fact]
    public void Transacao_ValorSempreAbsoluto()
    {
        var t = new Transacao(
            data: new DateOnly(2026, 10, 10),
            descricaoOriginal: "Despesa negativa",
            valor: new Dinheiro(-250m, "BRL"),
            natureza: NaturezaTransacao.Despesa,
            conta: CriarConta());

        t.Valor.Valor.Should().Be(250m);
    }

    // ---------- D-007: Contexto e Pessoa são enums ----------

    [Fact]
    public void Transacao_TemContextoEPessoa()
    {
        var t = CriarTransacao(
            NaturezaTransacao.Receita,
            contexto: Contexto.YouTube,
            pessoa: Pessoa.Conjuge);

        t.Contexto.Should().Be(Contexto.YouTube);
        t.Pessoa.Should().Be(Pessoa.Conjuge);
    }

    // ---------- Multi-eixo ----------

    [Fact]
    public void Transacao_PodeTerMultiplasTags()
    {
        var t = CriarTransacao(NaturezaTransacao.Despesa);
        var tag1 = new Tag("viagem");
        var tag2 = new Tag("trabalho");

        t.AdicionarTag(tag1);
        t.AdicionarTag(tag2);

        t.Tags.Should().HaveCount(2);
        t.Tags.Should().Contain(new[] { tag1, tag2 });
    }

    [Fact]
    public void AdicionarTag_Duplicada_NaoDeveDuplicar()
    {
        var t = CriarTransacao(NaturezaTransacao.Despesa);
        var tag = new Tag("viagem");

        t.AdicionarTag(tag);
        t.AdicionarTag(tag);

        t.Tags.Should().HaveCount(1);
    }

    [Fact]
    public void RemoverTag_RemoverTagExistente_RemoverDaLista()
    {
        var t = CriarTransacao(NaturezaTransacao.Despesa);
        var tag = new Tag("viagem");

        t.AdicionarTag(tag);
        t.RemoverTag(tag);

        t.Tags.Should().HaveCount(0);
    }

    [Fact]
    public void RemoverTag_RemoverTagInexistente_NaoDeveLancarExcecao()
    {
        var t = CriarTransacao(NaturezaTransacao.Despesa);
        var tag = new Tag("viagem");

        t.RemoverTag(tag);

        t.Tags.Should().HaveCount(0);
    }

    // ---------- Data ----------

    [Fact]
    public void Transacao_TemData()
    {
        var t = CriarTransacao(NaturezaTransacao.Despesa);

        t.Data.Should().Be(new DateOnly(2026, 10, 10));
    }

    // ---------- Descricao ----------

    [Fact]
    public void Transacao_DescricaoOriginal_TrimWhitespace()
    {
        var t = new Transacao(
            data: new DateOnly(2026, 10, 10),
            descricaoOriginal: "  Compra no supermercado  ",
            valor: new Dinheiro(50m, "BRL"),
            natureza: NaturezaTransacao.Despesa,
            conta: CriarConta());

        t.DescricaoOriginal.Should().Be("Compra no supermercado");
    }

    [Fact]
    public void Transacao_DescricaoOriginal_Vazia_DeveLancarExcecao()
    {
        var act = () => new Transacao(
            data: new DateOnly(2026, 10, 10),
            descricaoOriginal: "  ",
            valor: new Dinheiro(50m, "BRL"),
            natureza: NaturezaTransacao.Despesa,
            conta: CriarConta());

        act.Should().Throw<ArgumentException>();
    }

    // ---------- Classificacao ----------

    [Fact]
    public void Classificar_DefinirCategoriaContextoPessoaETags_MarcarComoClassificada()
    {
        var t = CriarTransacao(NaturezaTransacao.Despesa);
        var categoria = new Categoria("Alimentação");
        var tag = new Tag("supermercado");

        t.Classificar(
            categoria,
            Contexto.Pessoal,
            Pessoa.Eu,
            new[] { tag });

        t.Classificada.Should().BeTrue();
        t.Categoria.Should().Be(categoria);
        t.Contexto.Should().Be(Contexto.Pessoal);
        t.Pessoa.Should().Be(Pessoa.Eu);
        t.Tags.Should().Contain(tag);
    }

    [Fact]
    public void Desclassificar_RemoverCategoria_MarcarComoNaoClassificada()
    {
        var t = CriarTransacao(NaturezaTransacao.Despesa);
        var categoria = new Categoria("Alimentação");

        t.Classificar(categoria, Contexto.Pessoal, Pessoa.Eu);
        t.Desclassificar();

        t.Classificada.Should().BeFalse();
        t.Categoria.Should().BeNull();
    }

    // ---------- Observacao ----------

    [Fact]
    public void DefinirObservacao_TextoNaoNulo_DefinirObservacao()
    {
        var t = CriarTransacao(NaturezaTransacao.Despesa);

        t.DefinirObservacao("Observação de teste");

        t.Observacao.Should().Be("Observação de teste");
    }

    [Fact]
    public void DefinirObservacao_TextoVazio_LimparObservacao()
    {
        var t = CriarTransacao(NaturezaTransacao.Despesa);

        t.DefinirObservacao(null);

        t.Observacao.Should().BeNull();
    }

    // ---------- NormalizarDescricao ----------

    [Fact]
    public void NormalizarDescricao_TextoNaoNulo_DefinirDescricaoNormalizada()
    {
        var t = CriarTransacao(NaturezaTransacao.Despesa);

        t.NormalizarDescricao("Descrição normalizada");

        t.DescricaoNormalizada.Should().Be("Descrição normalizada");
    }

    [Fact]
    public void NormalizarDescricao_TextoVazio_DeveLancarExcecao()
    {
        var t = CriarTransacao(NaturezaTransacao.Despesa);

        var act = () => t.NormalizarDescricao("  ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CriarTransacao_DescricaoOriginal_DeveNormalizarAutomaticamente()
    {
        // Arrange
        var descricaoOriginal = "  PADARIA  ";

        // Act
        var transacao = new Transacao(
            new DateOnly(2026, 10, 12),
            descricaoOriginal,
            new Dinheiro(10),
            NaturezaTransacao.Receita,
            CriarConta());

        // Assert
        Assert.Equal("PADARIA", transacao.DescricaoOriginal);
        Assert.Equal(DescricaoNormalizador.Normalizar("PADARIA"), transacao.DescricaoNormalizada);
    }

    [Fact]
    public void Atualizar_DescricaoOriginal_DeveRenormalizar()
    {
        // Arrange
        var transacao = new Transacao(
            new DateOnly(2026, 10, 12),
            "PADARIA",
            new Dinheiro(10),
            NaturezaTransacao.Receita,
            CriarConta());
        var novaDescricao = "  Mercado  ";

        // Act
        transacao.Atualizar(
            new DateOnly(2026, 10, 12),
            novaDescricao,
            new Dinheiro(10),
            NaturezaTransacao.Receita,
            CriarConta(),
            Contexto.Pessoal,
            Pessoa.Eu,
            null);

        // Assert
        Assert.Equal("Mercado", transacao.DescricaoOriginal);
        Assert.Equal(DescricaoNormalizador.Normalizar("Mercado"), transacao.DescricaoNormalizada);
    }
}
