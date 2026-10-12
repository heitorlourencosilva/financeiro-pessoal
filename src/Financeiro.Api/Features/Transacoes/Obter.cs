using Financeiro.Domain.Entities;
using Financeiro.Domain.Enums;
using Financeiro.Infrastructure.Persistence;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Features.Transacoes;

public class Obter : Endpoint<Obter.FeatureRequest, TransacaoResponse>
{
    private readonly FinanceiroDbContext _context;

    public Obter(FinanceiroDbContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("/api/transacoes/{Id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(FeatureRequest request, CancellationToken ct)
    {
        var transacao = await _context.Transacoes
            .Include(t => t.Conta)
            .Include(t => t.Categoria)
            .SingleOrDefaultAsync(t => t.Id == request.Id, ct);

        if (transacao is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendOkAsync(TransacaoResponse.FromTransacao(transacao), ct);
    }

    public record FeatureRequest(Guid Id);
}
