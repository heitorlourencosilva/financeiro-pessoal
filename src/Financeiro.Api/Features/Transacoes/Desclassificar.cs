using Financeiro.Infrastructure.Persistence;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Features.Transacoes;

public class Desclassificar : EndpointWithoutRequest<TransacaoResponse>
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

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");

        var transacao = await _context.Transacoes
            .Include(t => t.Conta)
            .Include(t => t.Categoria)
            .Include(t => t.Tags)
            .SingleOrDefaultAsync(t => t.Id == id, ct);

        if (transacao is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        transacao.Desclassificar();

        await _context.SaveChangesAsync(ct);

        await SendOkAsync(TransacaoResponse.FromTransacao(transacao), ct);
    }
}
