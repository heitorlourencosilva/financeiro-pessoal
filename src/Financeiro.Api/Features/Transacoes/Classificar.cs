using Financeiro.Domain.Entities;
using Financeiro.Domain.Enums;
using Financeiro.Api.Infrastructure.Persistence;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Features.Transacoes;

public class Classificar : Endpoint<Classificar.FeatureRequest, Classificar.FeatureResponse>
{
    private readonly FinanceiroDbContext _context;

    public Classificar(FinanceiroDbContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Put("/api/transacoes/{Id}/classificar");
        AllowAnonymous();
    }

    public override async Task HandleAsync(FeatureRequest request, CancellationToken ct)
    {
        var transacao = await _context.Transacoes
            .Include(t => t.Conta)
            .Include(t => t.Categoria)
            .Include(t => t.Tags)
            .SingleOrDefaultAsync(t => t.Id == request.Id, ct);

        if (transacao is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        var categoria = await _context.Categorias
            .SingleOrDefaultAsync(c => c.Id == request.CategoriaId, ct);

        if (categoria is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        // D-012: se qualquer TagId não existir, retorna 404 (não ignora silenciosamente)
        var tagIds = request.TagIds ?? new List<Guid>();
        var tags = await _context.Tags
            .Where(t => tagIds.Contains(t.Id))
            .ToListAsync(ct);

        if (tags.Count != tagIds.Distinct().Count())
        {
            await SendNotFoundAsync(ct);
            return;
        }

        transacao.Classificar(categoria, request.Contexto, request.Pessoa, tags);

        await _context.SaveChangesAsync(ct);

        await SendOkAsync(FeatureResponse.FromTransacao(transacao), ct);
    }

    public record FeatureRequest(
        Guid Id,
        Guid CategoriaId,
        Contexto Contexto,
        Pessoa Pessoa,
        List<Guid>? TagIds = null);

    public record FeatureResponse(
        Guid Id,
        DateOnly Data,
        string Descricao,
        string? DescricaoNormalizada,
        decimal Valor,
        string Moeda,
        NaturezaTransacao Natureza,
        Guid ContaId,
        string ContaNome,
        Guid? CategoriaId,
        string? CategoriaNome,
        Contexto Contexto,
        Pessoa Pessoa,
        bool Classificada,
        string? Observacao,
        DateTime ImportadaEm,
        DateTime? ClassificadaEm)
    {
        public static FeatureResponse FromTransacao(Transacao t) => new(
            Id: t.Id,
            Data: t.Data,
            Descricao: t.DescricaoOriginal,
            DescricaoNormalizada: t.DescricaoNormalizada,
            Valor: t.Valor.Valor,
            Moeda: t.Valor.Moeda,
            Natureza: t.Natureza,
            ContaId: t.ContaId,
            ContaNome: t.Conta.Nome,
            CategoriaId: t.CategoriaId,
            CategoriaNome: t.Categoria?.Nome,
            Contexto: t.Contexto,
            Pessoa: t.Pessoa,
            Classificada: t.Classificada,
            Observacao: t.Observacao,
            ImportadaEm: t.ImportadaEm,
            ClassificadaEm: t.ClassificadaEm);
    }
}

public class ClassificarValidator : Validator<Classificar.FeatureRequest>
{
    public ClassificarValidator()
    {
        RuleFor(r => r.CategoriaId)
            .NotEmpty().WithMessage("A categoria é obrigatória.");

        RuleFor(r => r.Contexto)
            .IsInEnum().WithMessage("O contexto é inválido.");

        RuleFor(r => r.Pessoa)
            .IsInEnum().WithMessage("A pessoa é inválida.");
    }
}