using Financeiro.Infrastructure.Persistence;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Features.RegrasClassificacao;

public sealed class Listar : EndpointWithoutRequest<RegraClassificacaoResponse[]>
{
    private readonly FinanceiroDbContext _context;

    public Listar(FinanceiroDbContext context) => _context = context;

    public override void Configure()
    {
        Get("/api/regras-classificacao");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var regras = await _context.RegrasClassificacaoAutomatica
            .Include(r => r.Categoria)
            .OrderByDescending(r => r.Prioridade)
            .ThenBy(r => r.DescricaoCorrespondenciaNormalizada)
            .Select(r => RegraClassificacaoResponse.FromRegra(r))
            .ToArrayAsync(ct);

        await SendOkAsync(regras, ct);
    }
}
