using Financeiro.Domain.Entities;
using Financeiro.Domain.Enums;
using Financeiro.Infrastructure.Persistence;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Features.Transacoes;

public class ListarPendentes : EndpointWithoutRequest<TransacaoResponse[]>
{
    private readonly FinanceiroDbContext _context;

    public ListarPendentes(FinanceiroDbContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("/api/transacoes/pendentes");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var transacoes = await _context.Transacoes
            .Include(t => t.Conta)
            .Include(t => t.Categoria)
            .Where(t => !t.Classificada)
            .OrderByDescending(t => t.Data)
            .Select(t => TransacaoResponse.FromTransacao(t))
            .ToArrayAsync(ct);

        await SendOkAsync(transacoes, ct);
    }
}