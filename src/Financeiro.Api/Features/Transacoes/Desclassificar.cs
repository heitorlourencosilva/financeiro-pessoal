using Financeiro.Domain.Entities;
using Financeiro.Domain.Enums;
using Financeiro.Api.Infrastructure.Persistence;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Features.Transacoes;

public class Desclassificar : Endpoint<Desclassificar.FeatureRequest, Desclassificar.FeatureResponse>
{
    private readonly FinanceiroDbContext _context;

    public Desclassificar(FinanceiroDbContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Put("/api/transacoes/{Id}/desclassificar");
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

        transacao.Desclassificar();

        await _context.SaveChangesAsync(ct);

        await SendOkAsync(FeatureResponse.FromTransacao(transacao), ct);
    }

    public record FeatureRequest(Guid Id);

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