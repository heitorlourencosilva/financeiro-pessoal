using Financeiro.Domain.Enums;
using Financeiro.Domain.ValueObjects;

namespace Financeiro.Domain.Entities;

public sealed class Transacao
{
    public Guid Id { get; private set; }

    // Quando
    public DateOnly Data { get; private set; }

    // O quê
    public string DescricaoOriginal { get; private set; }
    public string? DescricaoNormalizada { get; private set; }
    public Dinheiro Valor { get; private set; }
    public NaturezaTransacao Natureza { get; private set; }

    // Onde
    public Guid ContaId { get; private set; }
    public Conta Conta { get; private set; }

    // Classificação
    public Guid? CategoriaId { get; private set; }
    public Categoria? Categoria { get; private set; }
    public Contexto Contexto { get; private set; }
    public Pessoa Pessoa { get; private set; }
    public ICollection<Tag> Tags { get; private set; }

    // Controle
    public bool Classificada { get; private set; }
    public string? Observacao { get; private set; }
    public DateTime ImportadaEm { get; private set; }
    public DateTime? ClassificadaEm { get; private set; }

    private Transacao()
    {
        DescricaoOriginal = null!;
        Conta = null!;
        Tags = new List<Tag>();
    }

    public Transacao(
        DateOnly data,
        string descricaoOriginal,
        Dinheiro valor,
        NaturezaTransacao natureza,
        Conta conta,
        Contexto contexto = Contexto.Pessoal,
        Pessoa pessoa = Pessoa.Eu)
    {
        if (string.IsNullOrWhiteSpace(descricaoOriginal))
            throw new ArgumentException("Descrição é obrigatória.", nameof(descricaoOriginal));

        if (conta is null)
            throw new ArgumentNullException(nameof(conta));

        if (valor.Valor == 0)
            throw new ArgumentException("Valor não pode ser zero.", nameof(valor));

        Id = Guid.NewGuid();
        Data = data;
        DescricaoOriginal = descricaoOriginal.Trim();
        Valor = valor.Abs();
        Natureza = natureza;
        Conta = conta;
        ContaId = conta.Id;
        Contexto = contexto;
        Pessoa = pessoa;
        Tags = new List<Tag>();
        Classificada = false;
        ImportadaEm = DateTime.UtcNow;
    }

    // --- Comportamentos ---

    public void Classificar(
        Categoria categoria,
        Contexto contexto,
        Pessoa pessoa,
        IEnumerable<Tag>? tags = null)
    {
        if (categoria is null)
            throw new ArgumentNullException(nameof(categoria));

        Categoria = categoria;
        CategoriaId = categoria.Id;
        Contexto = contexto;
        Pessoa = pessoa;

        if (tags is not null)
        {
            Tags.Clear();
            foreach (var tag in tags)
                Tags.Add(tag);
        }

        Classificada = true;
        ClassificadaEm = DateTime.UtcNow;
    }

    public void Desclassificar()
    {
        Categoria = null;
        CategoriaId = null;
        Classificada = false;
        ClassificadaEm = null;
    }

    public void NormalizarDescricao(string descricaoNormalizada)
    {
        if (string.IsNullOrWhiteSpace(descricaoNormalizada))
            throw new ArgumentException("Descrição normalizada é obrigatória.", nameof(descricaoNormalizada));

        DescricaoNormalizada = descricaoNormalizada.Trim();
    }

    public void AdicionarTag(Tag tag)
    {
        if (tag is null) throw new ArgumentNullException(nameof(tag));
        if (Tags.Any(t => t.Id == tag.Id)) return;
        Tags.Add(tag);
    }

    public void RemoverTag(Tag tag)
    {
        if (tag is null) throw new ArgumentNullException(nameof(tag));
        Tags.Remove(tag);
    }

    public void DefinirObservacao(string? observacao)
    {
        Observacao = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim();
    }

    public void Atualizar(
        DateOnly data,
        string descricaoOriginal,
        Dinheiro valor,
        NaturezaTransacao natureza,
        Conta conta,
        Contexto contexto,
        Pessoa pessoa,
        string? observacao)
    {
        if (string.IsNullOrWhiteSpace(descricaoOriginal))
            throw new ArgumentException("Descrição é obrigatória.", nameof(descricaoOriginal));

        if (conta is null)
            throw new ArgumentNullException(nameof(conta));

        if (valor.Valor == 0)
            throw new ArgumentException("Valor não pode ser zero.", nameof(valor));

        Data = data;
        DescricaoOriginal = descricaoOriginal.Trim();
        Valor = valor.Abs();
        Natureza = natureza;
        Conta = conta;
        ContaId = conta.Id;
        Contexto = contexto;
        Pessoa = pessoa;
        DefinirObservacao(observacao);
    }

    // --- Regras de negócio ---

    public bool EhNeutra() =>
        Natureza is NaturezaTransacao.Transferencia or NaturezaTransacao.Investimento;

    public bool AfetaFluxoDeCaixa() => !EhNeutra();

    public int SinalFluxo() => Natureza switch
    {
        NaturezaTransacao.Receita => 1,
        NaturezaTransacao.Despesa => -1,
        _ => 0
    };
}