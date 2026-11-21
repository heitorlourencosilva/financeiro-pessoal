using Financeiro.Infrastructure.Persistence;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Features.RegrasClassificacao;

public sealed class Desativar : EndpointWithoutRequest<RegraClassificacaoResponse>
{
    private readonly FinanceiroDbContext _context;

    public Desativar(FinanceiroDbContext context) => _context = context;

    public override void Configure()
    {
        Put("/api/regras-classificacao/{Id}/desativar");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var regra = await _context.RegrasClassificacaoAutomatica
            .Include(r => r.Categoria)
            .SingleOrDefaultAsync(r => r.Id == id, ct);

        if (regra is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        regra.Desativar();
        await _context.SaveChangesAsync(ct);
        await SendOkAsync(RegraClassificacaoResponse.FromRegra(regra), ct);
    }
}
