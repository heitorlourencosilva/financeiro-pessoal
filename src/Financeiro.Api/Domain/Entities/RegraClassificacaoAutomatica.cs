using Financeiro.Domain.Enums;
using Financeiro.Domain.Services;

namespace Financeiro.Domain.Entities;

public sealed class RegraClassificacaoAutomatica
{
    public Guid Id { get; private set; }
    public string DescricaoCorrespondenciaNormalizada { get; private set; }
    public Guid CategoriaId { get; private set; }
    public Categoria Categoria { get; private set; }
    public Contexto Contexto { get; private set; }
    public Pessoa Pessoa { get; private set; }
    public int Prioridade { get; private set; }
    public bool Ativa { get; private set; }
    public DateTime CriadaEm { get; private set; }
    public DateTime AtualizadaEm { get; private set; }

    private RegraClassificacaoAutomatica()
    {
        DescricaoCorrespondenciaNormalizada = null!;
        Categoria = null!;
    }

    public RegraClassificacaoAutomatica(
        string descricaoCorrespondencia,
        Categoria categoria,
        Contexto contexto,
        Pessoa pessoa,
        int prioridade)
    {
        if (string.IsNullOrWhiteSpace(descricaoCorrespondencia))
            throw new ArgumentException("A descrição para correspondência é obrigatória.", nameof(descricaoCorrespondencia));

        ArgumentNullException.ThrowIfNull(categoria);

        Id = Guid.NewGuid();
        DescricaoCorrespondenciaNormalizada = NormalizarDescricaoCorrespondencia(descricaoCorrespondencia);
        Categoria = categoria;
        CategoriaId = categoria.Id;
        Contexto = contexto;
        Pessoa = pessoa;
        Prioridade = prioridade;
        Ativa = true;
        CriadaEm = DateTime.UtcNow;
        AtualizadaEm = CriadaEm;
    }

    public void Atualizar(
        string descricaoCorrespondencia,
        Categoria categoria,
        Contexto contexto,
        Pessoa pessoa,
        int prioridade,
        bool ativa)
    {
        if (string.IsNullOrWhiteSpace(descricaoCorrespondencia))
            throw new ArgumentException("A descrição para correspondência é obrigatória.", nameof(descricaoCorrespondencia));

        ArgumentNullException.ThrowIfNull(categoria);

        DescricaoCorrespondenciaNormalizada = NormalizarDescricaoCorrespondencia(descricaoCorrespondencia);
        Categoria = categoria;
        CategoriaId = categoria.Id;
        Contexto = contexto;
        Pessoa = pessoa;
        Prioridade = prioridade;
        Ativa = ativa;
        AtualizadaEm = DateTime.UtcNow;
    }

    private static string NormalizarDescricaoCorrespondencia(string descricaoCorrespondencia)
    {
        var descricaoNormalizada = DescricaoNormalizador.Normalizar(descricaoCorrespondencia);
        if (!descricaoNormalizada.Any(char.IsLetterOrDigit))
            throw new ArgumentException(
                "A descrição para correspondência deve conter ao menos uma letra ou número após a normalização.",
                nameof(descricaoCorrespondencia));

        return descricaoNormalizada;
    }

    public void Desativar()
    {
        Ativa = false;
        AtualizadaEm = DateTime.UtcNow;
    }
}
