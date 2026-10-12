using Financeiro.Domain.Entities;
using Financeiro.Domain.Enums;
using Financeiro.Infrastructure.Persistence;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Features.Transacoes;

public class Listar : EndpointWithoutRequest<TransacaoResponse[]>
{
    private readonly FinanceiroDbContext _context;

    public Listar(FinanceiroDbContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("/api/transacoes");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var transacoes = await _context.Transacoes
            .Include(t => t.Conta)
            .Include(t => t.Categoria)
            .OrderByDescending(t => t.Data)
            .Select(t => TransacaoResponse.FromTransacao(t))
            .ToArrayAsync(ct);

        await SendOkAsync(transacoes, ct);
    }
}
