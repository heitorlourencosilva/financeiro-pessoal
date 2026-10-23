using Financeiro.Domain.Enums;
using Financeiro.Infrastructure.Persistence;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Features.Relatorios.PorConta;

public class PorConta : Endpoint<RelatorioPorContaRequest, RelatorioPorContaResponse>
{
    private readonly FinanceiroDbContext _context;

    public PorConta(FinanceiroDbContext context) => _context = context;

    public override void Configure()
    {
        Get("/api/relatorios/por-conta");
        AllowAnonymous();
    }

    public override async Task HandleAsync(RelatorioPorContaRequest req, CancellationToken ct)
    {
        // 1. Contas ativas (relatório lista todas, inclusive sem movimento no período)
        var contas = await _context.Contas
            .Where(c => c.Ativa)
            .OrderBy(c => c.Nome)
            .Select(c => new { c.Id, c.Nome })
            .ToListAsync(ct);

        // 2. Agregação por conta + natureza, respeitando o período
        var query = _context.Transacoes.AsQueryable();

        if (req.DataInicio.HasValue)
            query = query.Where(t => t.Data >= req.DataInicio.Value);

        if (req.DataFim.HasValue)
            query = query.Where(t => t.Data <= req.DataFim.Value);

        var agregados = await query
            .GroupBy(t => new { t.ContaId, t.Natureza })
            .Select(g => new
            {
                g.Key.ContaId,
                g.Key.Natureza,
                Total = g.Sum(t => t.Valor.Valor)
            })
            .ToListAsync(ct);

        // 3. Monta as linhas: uma por conta, com zeros quando não há movimento
        var linhas = new List<LinhaRelatorioConta>(contas.Count);

        foreach (var conta in contas)
        {
            var receitas = agregados
                .Where(a => a.ContaId == conta.Id && a.Natureza == NaturezaTransacao.Receita)
                .Sum(a => a.Total);

            var despesas = agregados
                .Where(a => a.ContaId == conta.Id && a.Natureza == NaturezaTransacao.Despesa)
                .Sum(a => a.Total);

            var transferencias = agregados
                .Where(a => a.ContaId == conta.Id && a.Natureza == NaturezaTransacao.Transferencia)
                .Sum(a => a.Total);

            var investimentos = agregados
                .Where(a => a.ContaId == conta.Id && a.Natureza == NaturezaTransacao.Investimento)
                .Sum(a => a.Total);

            // D-001 / D-003: neutros não entram no saldo
            var saldo = receitas - despesas;

            linhas.Add(new LinhaRelatorioConta(
                conta.Id, conta.Nome,
                receitas, despesas, transferencias, investimentos, saldo));
        }

        // 4. Total geral no final
        var total = new LinhaRelatorioConta(
            Guid.Empty, "TOTAL",
            linhas.Sum(l => l.TotalReceitas),
            linhas.Sum(l => l.TotalDespesas),
            linhas.Sum(l => l.TotalTransferencias),
            linhas.Sum(l => l.TotalInvestimentos),
            linhas.Sum(l => l.SaldoPeriodo));

        await SendOkAsync(new RelatorioPorContaResponse(linhas, total), ct);
    }
}

public record RelatorioPorContaRequest(
    DateOnly? DataInicio = null,
    DateOnly? DataFim = null);