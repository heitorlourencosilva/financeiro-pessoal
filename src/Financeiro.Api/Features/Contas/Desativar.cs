using Financeiro.Domain.Entities;
using Financeiro.Domain.Enums;
using Financeiro.Api.Infrastructure.Persistence;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Features.Contas;

public class Desativar : Endpoint<Desativar.FeatureRequest, Desativar.FeatureResponse>
{
    private readonly FinanceiroDbContext _context;

    public Desativar(FinanceiroDbContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Delete("/api/contas/{Id}/desativar");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Desativar.FeatureRequest request, CancellationToken ct)
    {
        var conta = await _context.Contas
            .SingleOrDefaultAsync(c => c.Id == request.Id, ct);

        if (conta is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        conta.Desativar();
        await _context.SaveChangesAsync(ct);

        await SendOkAsync(new FeatureResponse(conta.Id, conta.Nome, conta.Tipo, conta.Instituicao, conta.Ativa, conta.CriadaEm), ct);
    }

    public record FeatureRequest(Guid Id);

    public record FeatureResponse(Guid Id, string Nome, TipoConta Tipo, string? Instituicao, bool Ativa, DateTime CriadaEm);
}
