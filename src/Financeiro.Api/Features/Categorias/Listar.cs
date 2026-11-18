using Financeiro.Infrastructure.Persistence;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Features.Categorias;

public class Listar : EndpointWithoutRequest<Listar.FeatureResponse[]>
{
    private readonly FinanceiroDbContext _context;

    public Listar(FinanceiroDbContext context) => _context = context;

    public override void Configure()
    {
        Get("/api/categorias");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var categorias = await _context.Categorias
            .Where(categoria => categoria.Ativa)
            .OrderBy(categoria => categoria.Nome)
            .Select(categoria => new FeatureResponse(
                categoria.Id,
                categoria.Nome,
                categoria.CategoriaPaiId))
            .ToArrayAsync(ct);

        await SendOkAsync(categorias, ct);
    }

    public record FeatureResponse(Guid Id, string Nome, Guid? CategoriaPaiId);
}
