using Financeiro.Domain.Enums;
using Financeiro.Features.Classificacao;
using Financeiro.Infrastructure.Persistence;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Features.Transacoes;

public sealed class SugerirClassificacao : EndpointWithoutRequest<SugerirClassificacao.FeatureResponse>
{
    private readonly FinanceiroDbContext _context;
    private readonly ServicoClassificacao _servicoClassificacao;

    public SugerirClassificacao(
        FinanceiroDbContext context,
        ServicoClassificacao servicoClassificacao)
    {
        _context = context;
        _servicoClassificacao = servicoClassificacao;
    }

    public override void Configure()
    {
        Get("/api/transacoes/{Id}/sugestao-classificacao");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var transacao = await _context.Transacoes
            .SingleOrDefaultAsync(t => t.Id == id && !t.Classificada, ct);

        if (transacao is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        var sugestao = await _servicoClassificacao.SugerirAsync(transacao, ct);
        var response = sugestao is null
            ? new FeatureResponse(false, null, null, null, null, null, null)
            : new FeatureResponse(
                true,
                sugestao.CategoriaId,
                sugestao.CategoriaNome,
                sugestao.Contexto,
                sugestao.Pessoa,
                sugestao.Origem,
                sugestao.RegraId);

        await SendOkAsync(response, ct);
    }

    public sealed record FeatureResponse(
        bool TemSugestao,
        Guid? CategoriaId,
        string? CategoriaNome,
        Contexto? Contexto,
        Pessoa? Pessoa,
        string? Origem,
        Guid? RegraId);
}
