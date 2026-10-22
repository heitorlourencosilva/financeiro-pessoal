using Financeiro.Domain.Entities;
using Financeiro.Domain.Enums;
using Financeiro.Infrastructure.Persistence;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Features.Transacoes;

public class Listar : Endpoint<FiltrosTransacoesRequest, TransacaoResponse[]>
{
    private readonly FinanceiroDbContext _context;

    public Listar(FinanceiroDbContext context) => _context = context;

    public override void Configure()
    {
        Get("/api/transacoes");
        AllowAnonymous();
    }

    public override async Task HandleAsync(FiltrosTransacoesRequest req, CancellationToken ct)
    {
        var query = _context.Transacoes
            .Include(t => t.Conta)
            .Include(t => t.Categoria)
            .AsQueryable();

        if (req.DataInicio.HasValue) query = query.Where(t => t.Data >= req.DataInicio.Value);
        if (req.DataFim.HasValue) query = query.Where(t => t.Data <= req.DataFim.Value);
        if (req.CategoriaId.HasValue) query = query.Where(t => t.CategoriaId == req.CategoriaId.Value);
        if (req.Contexto.HasValue) query = query.Where(t => t.Contexto == req.Contexto.Value);
        if (req.Pessoa.HasValue) query = query.Where(t => t.Pessoa == req.Pessoa.Value);
        if (req.TagId.HasValue) query = query.Where(t => t.Tags.Any(tg => tg.Id == req.TagId.Value));

        var transacoes = await query
            .OrderByDescending(t => t.Data)
            .Select(t => TransacaoResponse.FromTransacao(t))
            .ToArrayAsync(ct);

        await SendOkAsync(transacoes, ct);
    }
}

public record FiltrosTransacoesRequest(
    DateOnly? DataInicio = null,
    DateOnly? DataFim = null,
    Guid? CategoriaId = null,
    Contexto? Contexto = null,
    Pessoa? Pessoa = null,
    Guid? TagId = null);