using Financeiro.Infrastructure.Persistence;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Features.Tags;

public class Listar : EndpointWithoutRequest<Listar.FeatureResponse[]>
{
    private readonly FinanceiroDbContext _context;

    public Listar(FinanceiroDbContext context) => _context = context;

    public override void Configure()
    {
        Get("/api/tags");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var tags = await _context.Tags
            .OrderBy(tag => tag.Nome)
            .Select(tag => new FeatureResponse(tag.Id, tag.Nome))
            .ToArrayAsync(ct);

        await SendOkAsync(tags, ct);
    }

    public record FeatureResponse(Guid Id, string Nome);
}
