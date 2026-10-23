using Financeiro.Domain.Enums;
using Financeiro.Infrastructure.Persistence;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Features.Relatorios.Composicao;

public class Composicao : Endpoint<ComposicaoRequest, ComposicaoResponse>
{
    private readonly FinanceiroDbContext _context;

    public Composicao(FinanceiroDbContext context) => _context = context;

    public override void Configure()
    {
        Get("/api/relatorios/composicao");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ComposicaoRequest req, CancellationToken ct)
    {
        var query = _context.Transacoes
            .Where(t => t.Natureza == NaturezaTransacao.Receita
                     || t.Natureza == NaturezaTransacao.Despesa);

        if (req.DataInicio.HasValue)
            query = query.Where(t => t.Data >= req.DataInicio.Value);

        if (req.DataFim.HasValue)
            query = query.Where(t => t.Data <= req.DataFim.Value);

        // Agrupa por Natureza + Categoria (null = "Sem categoria")
        var agregados = await query
            .GroupBy(t => new { t.Natureza, t.CategoriaId })
            .Select(g => new
            {
                g.Key.Natureza,
                g.Key.CategoriaId,
                Total = g.Sum(t => t.Valor.Valor)
            })
            .ToListAsync(ct);

        // Nomes das categorias (uma query só)
        var categoriaIds = agregados
            .Where(a => a.CategoriaId.HasValue)
            .Select(a => a.CategoriaId!.Value)
            .Distinct()
            .ToList();

        var nomesCategorias = categoriaIds.Count == 0 ? []
            : await _context.Categorias
                .Where(c => categoriaIds.Contains(c.Id))
                .Select(c => new { c.Id, c.Nome })
                .ToDictionaryAsync(c => c.Id, c => c.Nome, ct);

        var receitas = agregados
            .Where(a => a.Natureza == NaturezaTransacao.Receita)
            .Select(a => new LinhaComposicao(
                a.CategoriaId,
                a.CategoriaId.HasValue
                    ? nomesCategorias.GetValueOrDefault(a.CategoriaId.Value, "Desconhecida")
                    : "Sem categoria",
                a.Total))
            .OrderByDescending(l => l.Total)
            .ToList();

        var despesas = agregados
            .Where(a => a.Natureza == NaturezaTransacao.Despesa)
            .Select(a => new LinhaComposicao(
                a.CategoriaId,
                a.CategoriaId.HasValue
                    ? nomesCategorias.GetValueOrDefault(a.CategoriaId.Value, "Desconhecida")
                    : "Sem categoria",
                a.Total))
            .OrderByDescending(l => l.Total)
            .ToList();

        await SendOkAsync(new ComposicaoResponse(receitas, despesas), ct);
    }
}

public record ComposicaoRequest(
    DateOnly? DataInicio = null,
    DateOnly? DataFim = null);
