using Financeiro.Domain.Enums;
using Financeiro.Infrastructure.Persistence;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Features.Relatorios.Comparacao;

public class Comparacao : Endpoint<ComparacaoRequest, ComparacaoResponse>
{
    private readonly FinanceiroDbContext _context;

    public Comparacao(FinanceiroDbContext context) => _context = context;

    public override void Configure()
    {
        Get("/api/relatorios/comparacao");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ComparacaoRequest req, CancellationToken ct)
    {
        if (!req.PeriodoAInicio.HasValue || !req.PeriodoAFim.HasValue
         || !req.PeriodoBInicio.HasValue || !req.PeriodoBFim.HasValue)
            ThrowError("Os quatro parâmetros de período são obrigatórios.");

        var aInicio = req.PeriodoAInicio!.Value;
        var aFim = req.PeriodoAFim!.Value;
        var bInicio = req.PeriodoBInicio!.Value;
        var bFim = req.PeriodoBFim!.Value;

        if (aInicio > aFim) ThrowError("PeriodoAInicio não pode ser maior que PeriodoAFim.");
        if (bInicio > bFim) ThrowError("PeriodoBInicio não pode ser maior que PeriodoBFim.");

        // D-001/D-003: só Receita/Despesa
        var baseQuery = _context.Transacoes
            .Where(t => t.Natureza == NaturezaTransacao.Receita
                     || t.Natureza == NaturezaTransacao.Despesa);

        // Resumo de cada período
        var resumoA = await CalcularResumoAsync(baseQuery, aInicio, aFim, ct);
        var resumoB = await CalcularResumoAsync(baseQuery, bInicio, bFim, ct);

        // Agregação separada por período — à prova de sobreposição A/B
        var agregadosA = await baseQuery
            .Where(t => t.Data >= aInicio && t.Data <= aFim)
            .GroupBy(t => new { t.Natureza, t.CategoriaId })
            .Select(g => new AgregadoComparacao
            (
                g.Key.Natureza,
                g.Key.CategoriaId,
                g.Sum(t => t.Valor.Valor)
            ))
            .ToListAsync(ct);

        var agregadosB = await baseQuery
            .Where(t => t.Data >= bInicio && t.Data <= bFim)
            .GroupBy(t => new { t.Natureza, t.CategoriaId })
            .Select(g => new AgregadoComparacao
            (
                g.Key.Natureza,
                g.Key.CategoriaId,
                g.Sum(t => t.Valor.Valor)
            ))
            .ToListAsync(ct);

        // União das categorias presentes em A ou B
        var categoriaIds = agregadosA.Select(a => a.CategoriaId)
            .Concat(agregadosB.Select(b => b.CategoriaId))
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var nomesCategorias = categoriaIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _context.Categorias
                .Where(c => categoriaIds.Contains(c.Id))
                .Select(c => new { c.Id, c.Nome })
                .ToDictionaryAsync(c => c.Id, c => c.Nome, ct);

        var receitas = MontarLinhas(agregadosA, agregadosB, NaturezaTransacao.Receita, nomesCategorias);
        var despesas = MontarLinhas(agregadosA, agregadosB, NaturezaTransacao.Despesa, nomesCategorias);

        await SendOkAsync(new ComparacaoResponse(
            resumoA, resumoB, receitas, despesas), ct);
    }

    private static async Task<PeriodoResumo> CalcularResumoAsync(
        IQueryable<Domain.Entities.Transacao> baseQuery,
        DateOnly inicio,
        DateOnly fim,
        CancellationToken ct)
    {
        var totais = await baseQuery
            .Where(t => t.Data >= inicio && t.Data <= fim)
            .GroupBy(t => t.Natureza)
            .Select(g => new { Natureza = g.Key, Total = g.Sum(t => t.Valor.Valor) })
            .ToListAsync(ct);

        var receitas = totais.Where(t => t.Natureza == NaturezaTransacao.Receita).Sum(t => t.Total);
        var despesas = totais.Where(t => t.Natureza == NaturezaTransacao.Despesa).Sum(t => t.Total);

        return new PeriodoResumo(inicio, fim, receitas, despesas, receitas - despesas);
    }

    private record AgregadoComparacao(NaturezaTransacao Natureza, Guid? CategoriaId, decimal Total);
    private static List<LinhaComparacao> MontarLinhas(
    List<AgregadoComparacao> agregadosA,
    List<AgregadoComparacao> agregadosB,
    NaturezaTransacao natureza,
    Dictionary<Guid, string> nomesCategorias)
    {
        // União das categorias que aparecem em A ou B para essa natureza
        var chaves = agregadosA
            .Where(a => a.Natureza == natureza)
            .Select(a => a.CategoriaId)
            .Concat(agregadosB
                .Where(b => b.Natureza == natureza)
                .Select(b => b.CategoriaId))
            .Distinct()
            .ToList();

        var linhas = new List<LinhaComparacao>(chaves.Count);

        foreach (var categoriaId in chaves)
        {
            var totalA = agregadosA
                .Where(a => a.Natureza == natureza && a.CategoriaId == categoriaId)
                .Sum(a => a.Total);

            var totalB = agregadosB
                .Where(b => b.Natureza == natureza && b.CategoriaId == categoriaId)
                .Sum(b => b.Total);

            var nome = categoriaId.HasValue
                ? nomesCategorias.GetValueOrDefault(categoriaId.Value, "Desconhecida")
                : "Sem categoria";

            var diferenca = totalB - totalA;
            decimal? variacao = totalA == 0m ? null : (diferenca / totalA) * 100m;

            linhas.Add(new LinhaComparacao(
                categoriaId, nome, totalA, totalB, diferenca, variacao));
        }

        return linhas
            .OrderByDescending(l => Math.Abs(l.Diferenca))
            .ToList();
    }
}

public record ComparacaoRequest(
    DateOnly? PeriodoAInicio = null,
    DateOnly? PeriodoAFim = null,
    DateOnly? PeriodoBInicio = null,
    DateOnly? PeriodoBFim = null);