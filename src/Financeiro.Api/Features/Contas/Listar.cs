using Financeiro.Domain.Entities;
using Financeiro.Domain.Enums;
using Financeiro.Api.Infrastructure.Persistence;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Features.Contas;

public class Listar : EndpointWithoutRequest<Listar.Response[]>
{
    private readonly FinanceiroDbContext _context;

    public Listar(FinanceiroDbContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("/api/contas");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var contas = await _context.Contas
            .OrderBy(c => c.Nome)
            .Select(c => new Response(c.Id, c.Nome, c.Tipo, c.Instituicao, c.Ativa, c.CriadaEm))
            .ToArrayAsync(ct);

        await SendOkAsync(contas, ct);
    }

    public new record Response(Guid Id, string Nome, TipoConta Tipo, string? Instituicao, bool Ativa, DateTime CriadaEm);
}
