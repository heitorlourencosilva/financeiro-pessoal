using Financeiro.Domain.Entities;
using Financeiro.Domain.Enums;
using Financeiro.Domain.Services;
using Financeiro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Features.Classificacao;

public sealed class ServicoClassificacao(FinanceiroDbContext context)
{
    public async Task AplicarRegrasAutomaticasAsync(
        IEnumerable<Transacao> transacoes,
        CancellationToken cancellationToken)
    {
        var transacoesElegiveis = transacoes
            .Where(t => !t.EhNeutra() && !t.Classificada && !string.IsNullOrWhiteSpace(t.DescricaoNormalizada))
            .ToArray();

        if (transacoesElegiveis.Length == 0)
            return;

        var regras = await RegrasAtivasAsync(cancellationToken);
        foreach (var transacao in transacoesElegiveis)
        {
            var regra = EncontrarRegra(transacao.DescricaoNormalizada!, regras);
            if (regra is not null)
                transacao.Classificar(regra.Categoria, regra.Contexto, regra.Pessoa);
        }
    }

    public async Task<SugestaoClassificacao?> SugerirAsync(
        Transacao transacao,
        CancellationToken cancellationToken)
    {
        if (transacao.EhNeutra() || string.IsNullOrWhiteSpace(transacao.DescricaoNormalizada))
            return null;

        var regra = EncontrarRegra(
            transacao.DescricaoNormalizada!,
            await RegrasAtivasAsync(cancellationToken));

        if (regra is not null)
            return new SugestaoClassificacao(
                regra.CategoriaId,
                regra.Categoria.Nome,
                regra.Contexto,
                regra.Pessoa,
                "RegraAutomatica",
                regra.Id);

        var historico = await context.Transacoes
            .AsNoTracking()
            .Where(t => t.Id != transacao.Id &&
                        t.Classificada &&
                        t.Natureza == transacao.Natureza &&
                        t.CategoriaId != null &&
                        t.Categoria!.Ativa &&
                        t.DescricaoNormalizada != null)
            .Select(t => new HistoricoClassificacao(
                t.DescricaoNormalizada!,
                t.CategoriaId!.Value,
                t.Categoria!.Nome,
                t.Contexto,
                t.Pessoa,
                t.ClassificadaEm))
            .ToListAsync(cancellationToken);

        var maisSimilar = historico
            .Select(item => new
            {
                Item = item,
                Similaridade = DescricaoCorrespondencia.Similaridade(
                    transacao.DescricaoNormalizada!,
                    item.DescricaoNormalizada)
            })
            .Where(item => item.Similaridade > 0)
            .OrderByDescending(item => item.Similaridade)
            .ThenByDescending(item => item.Item.ClassificadaEm)
            .FirstOrDefault();

        return maisSimilar is null
            ? null
            : new SugestaoClassificacao(
                maisSimilar.Item.CategoriaId,
                maisSimilar.Item.CategoriaNome,
                maisSimilar.Item.Contexto,
                maisSimilar.Item.Pessoa,
                "Historico",
                null);
    }

    private async Task<List<RegraClassificacaoAutomatica>> RegrasAtivasAsync(CancellationToken cancellationToken) =>
        await context.RegrasClassificacaoAutomatica
            .Include(r => r.Categoria)
            .Where(r => r.Ativa && r.Categoria.Ativa)
            .OrderByDescending(r => r.Prioridade)
            .ThenByDescending(r => r.DescricaoCorrespondenciaNormalizada.Length)
            .ThenBy(r => r.Id)
            .ToListAsync(cancellationToken);

    private static RegraClassificacaoAutomatica? EncontrarRegra(
        string descricaoNormalizada,
        IEnumerable<RegraClassificacaoAutomatica> regras) =>
        regras.FirstOrDefault(regra => DescricaoCorrespondencia.ContemTermo(
            descricaoNormalizada,
            regra.DescricaoCorrespondenciaNormalizada));

    public sealed record SugestaoClassificacao(
        Guid CategoriaId,
        string CategoriaNome,
        Contexto Contexto,
        Pessoa Pessoa,
        string Origem,
        Guid? RegraId);

    private sealed record HistoricoClassificacao(
        string DescricaoNormalizada,
        Guid CategoriaId,
        string CategoriaNome,
        Contexto Contexto,
        Pessoa Pessoa,
        DateTime? ClassificadaEm);
}
