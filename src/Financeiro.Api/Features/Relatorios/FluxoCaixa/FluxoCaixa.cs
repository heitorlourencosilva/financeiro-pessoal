using Financeiro.Domain.Enums;
using Financeiro.Infrastructure.Persistence;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Features.Relatorios.FluxoCaixa;

public class FluxoCaixa : Endpoint<FluxoCaixaRequest, FluxoCaixaResponse>
{
    private readonly FinanceiroDbContext _context;

    public FluxoCaixa(FinanceiroDbContext context) => _context = context;

    public override void Configure()
    {
        Get("/api/relatorios/fluxo-caixa");
        AllowAnonymous();
    }

    public override async Task HandleAsync(FluxoCaixaRequest req, CancellationToken ct)
    {
        if (!req.DataInicio.HasValue || !req.DataFim.HasValue)
            ThrowError("dataInicio e dataFim são obrigatórios.");

        var inicio = req.DataInicio!.Value;
        var fim = req.DataFim!.Value;

        if (inicio > fim)
            ThrowError("dataInicio não pode ser maior que dataFim.");

        // D-001 / D-003: só Receita/Despesa entram no fluxo
        var baseQuery = _context.Transacoes
            .Where(t => t.Natureza == NaturezaTransacao.Receita
                     || t.Natureza == NaturezaTransacao.Despesa);

        // Saldo inicial: tudo antes de `inicio`
        var saldoInicial = await baseQuery
            .Where(t => t.Data < inicio)
            .SumAsync(t => t.Natureza == NaturezaTransacao.Receita
                ? t.Valor.Valor
                : -t.Valor.Valor, ct);

        // Movimento diário no período
        var movimentos = await baseQuery
            .Where(t => t.Data >= inicio && t.Data <= fim)
            .GroupBy(t => t.Data)
            .Select(g => new
            {
                Data = g.Key,
                Entradas = g.Sum(t => t.Natureza == NaturezaTransacao.Receita ? t.Valor.Valor : 0m),
                Saidas = g.Sum(t => t.Natureza == NaturezaTransacao.Despesa ? t.Valor.Valor : 0m)
            })
            .OrderBy(x => x.Data)
            .ToListAsync(ct);

        // Monta o diário com saldo acumulado
        var diario = new List<LinhaFluxoDiario>(movimentos.Count);
        var acumulado = saldoInicial;

        foreach (var m in movimentos)
        {
            acumulado += m.Entradas - m.Saidas;
            diario.Add(new LinhaFluxoDiario(m.Data, m.Entradas, m.Saidas, acumulado));
        }

        var totalEntradas = movimentos.Sum(m => m.Entradas);
        var totalSaidas = movimentos.Sum(m => m.Saidas);
        var saldoFinal = saldoInicial + totalEntradas - totalSaidas;

        await SendOkAsync(new FluxoCaixaResponse(
            inicio, fim,
            saldoInicial,
            totalEntradas,
            totalSaidas,
            saldoFinal,
            diario), ct);
    }
}

public record FluxoCaixaRequest(
    DateOnly? DataInicio = null,
    DateOnly? DataFim = null);