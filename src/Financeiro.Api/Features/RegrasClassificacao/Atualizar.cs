using Financeiro.Domain.Enums;
using Financeiro.Infrastructure.Persistence;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Features.RegrasClassificacao;

public sealed class Atualizar : Endpoint<Atualizar.FeatureRequest, RegraClassificacaoResponse>
{
    private readonly FinanceiroDbContext _context;

    public Atualizar(FinanceiroDbContext context) => _context = context;

    public override void Configure()
    {
        Put("/api/regras-classificacao/{Id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(FeatureRequest request, CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var regra = await _context.RegrasClassificacaoAutomatica
            .Include(r => r.Categoria)
            .SingleOrDefaultAsync(r => r.Id == id, ct);

        if (regra is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        var categoria = await _context.Categorias
            .SingleOrDefaultAsync(c => c.Id == request.CategoriaId && c.Ativa, ct);
        if (categoria is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        regra.Atualizar(
            request.DescricaoCorrespondencia,
            categoria,
            request.Contexto,
            request.Pessoa,
            request.Prioridade,
            request.Ativa);

        await _context.SaveChangesAsync(ct);
        await SendOkAsync(RegraClassificacaoResponse.FromRegra(regra), ct);
    }

    public sealed record FeatureRequest(
        string DescricaoCorrespondencia,
        Guid CategoriaId,
        Contexto Contexto,
        Pessoa Pessoa,
        int Prioridade,
        bool Ativa);
}

public sealed class AtualizarValidator : Validator<Atualizar.FeatureRequest>
{
    public AtualizarValidator()
    {
        RuleFor(r => r.DescricaoCorrespondencia)
            .NotEmpty().WithMessage("A descrição para correspondência é obrigatória.")
            .MaximumLength(200).WithMessage("A descrição para correspondência deve ter no máximo 200 caracteres.");

        RuleFor(r => r.CategoriaId)
            .NotEmpty().WithMessage("A categoria é obrigatória.");

        RuleFor(r => r.Contexto)
            .IsInEnum().WithMessage("O contexto é inválido.");

        RuleFor(r => r.Pessoa)
            .IsInEnum().WithMessage("A pessoa é inválida.");
    }
}
