using Financeiro.Domain.Entities;
using Financeiro.Domain.Enums;

namespace Financeiro.Features.RegrasClassificacao;

public sealed record RegraClassificacaoResponse(
    Guid Id,
    string DescricaoCorrespondenciaNormalizada,
    Guid CategoriaId,
    string CategoriaNome,
    Contexto Contexto,
    Pessoa Pessoa,
    int Prioridade,
    bool Ativa,
    DateTime CriadaEm,
    DateTime AtualizadaEm)
{
    public static RegraClassificacaoResponse FromRegra(RegraClassificacaoAutomatica regra) => new(
        regra.Id,
        regra.DescricaoCorrespondenciaNormalizada,
        regra.CategoriaId,
        regra.Categoria.Nome,
        regra.Contexto,
        regra.Pessoa,
        regra.Prioridade,
        regra.Ativa,
        regra.CriadaEm,
        regra.AtualizadaEm);
}
