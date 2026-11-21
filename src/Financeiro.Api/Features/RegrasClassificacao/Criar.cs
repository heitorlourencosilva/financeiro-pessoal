using Financeiro.Domain.Entities;
using Financeiro.Domain.Enums;
using Financeiro.Infrastructure.Persistence;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Features.RegrasClassificacao;

public sealed class Criar : Endpoint<Criar.FeatureRequest, RegraClassificacaoResponse>
{
    private readonly FinanceiroDbContext _context;

    public Criar(FinanceiroDbContext context) => _context = context;

    public override void Configure()
    {
        Post("/api/regras-classificacao");
        AllowAnonymous();
    }

    public override async Task HandleAsync(FeatureRequest request, CancellationToken ct)
    {
        var categoria = await _context.Categorias
            .SingleOrDefaultAsync(c => c.Id == request.CategoriaId && c.Ativa, ct);
        if (categoria is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        var regra = new RegraClassificacaoAutomatica(
            request.DescricaoCorrespondencia,
            categoria,
            request.Contexto,
            request.Pessoa,
            request.Prioridade);

        _context.RegrasClassificacaoAutomatica.Add(regra);
        await _context.SaveChangesAsync(ct);
        await SendOkAsync(RegraClassificacaoResponse.FromRegra(regra), ct);
    }

    public sealed record FeatureRequest(
        string DescricaoCorrespondencia,
        Guid CategoriaId,
        Contexto Contexto,
        Pessoa Pessoa,
        int Prioridade);
}

public sealed class CriarValidator : Validator<Criar.FeatureRequest>
{
    public CriarValidator()
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
