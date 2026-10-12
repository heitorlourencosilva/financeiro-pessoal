using Financeiro.Domain.Entities;
using Financeiro.Domain.Enums;
using Financeiro.Infrastructure.Persistence;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Features.Transacoes;

public class Desclassificar : Endpoint<Desclassificar.FeatureRequest, TransacaoResponse>
{
    private readonly FinanceiroDbContext _context;

    public Desclassificar(FinanceiroDbContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Put("/api/transacoes/{Id}/desclassificar");
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

        transacao.Desclassificar();

        await _context.SaveChangesAsync(ct);

        await SendOkAsync(TransacaoResponse.FromTransacao(transacao), ct);
    }

    public record FeatureRequest(Guid Id);
}