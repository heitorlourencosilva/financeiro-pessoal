using Financeiro.Domain.Entities;
using Financeiro.Domain.Enums;
using Financeiro.Infrastructure.Persistence;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Features.Contas;

public class Obter : Endpoint<Obter.FeatureRequest, Obter.FeatureResponse>
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

    public override async Task HandleAsync(Obter.FeatureRequest request, CancellationToken ct)
    {
        var conta = await _context.Contas
            .SingleOrDefaultAsync(c => c.Id == request.Id, ct);

        if (conta is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendOkAsync(new FeatureResponse(conta.Id, conta.Nome, conta.Tipo, conta.Instituicao, conta.Ativa, conta.CriadaEm), ct);
    }

    public record FeatureRequest(Guid Id);

    public record FeatureResponse(Guid Id, string Nome, TipoConta Tipo, string? Instituicao, bool Ativa, DateTime CriadaEm);
}
