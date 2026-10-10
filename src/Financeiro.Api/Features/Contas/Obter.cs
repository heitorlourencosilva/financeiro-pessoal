using Financeiro.Domain.Entities;
using Financeiro.Domain.Enums;
using Financeiro.Api.Infrastructure.Persistence;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Features.Contas;

public class Obter : Endpoint<Obter.Request, Obter.Response>
{
    private readonly FinanceiroDbContext _context;

    public Obter(FinanceiroDbContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("/api/contas/{Id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Obter.Request request, CancellationToken ct)
    {
        var conta = await _context.Contas
            .SingleOrDefaultAsync(c => c.Id == request.Id, ct);

        if (conta is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendOkAsync(new Response(conta.Id, conta.Nome, conta.Tipo, conta.Instituicao, conta.Ativa, conta.CriadaEm), ct);
    }

    public record Request(Guid Id);

    public record Response(Guid Id, string Nome, TipoConta Tipo, string? Instituicao, bool Ativa, DateTime CriadaEm);
}
