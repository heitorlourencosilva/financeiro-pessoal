using Financeiro.Domain.Entities;
using Financeiro.Domain.Enums;
using Financeiro.Infrastructure.Persistence;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Features.Contas;

public class Atualizar : Endpoint<Atualizar.FeatureRequest, Atualizar.FeatureResponse>
{
    private readonly FinanceiroDbContext _context;

    public Atualizar(FinanceiroDbContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Put("/api/contas/{Id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Atualizar.FeatureRequest request, CancellationToken ct)
    {
        var conta = await _context.Contas
            .SingleOrDefaultAsync(c => c.Id == request.Id, ct);

        if (conta is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        conta.Renomear(request.Nome);

        await _context.SaveChangesAsync(ct);

        await SendOkAsync(new FeatureResponse(conta.Id, conta.Nome, conta.Tipo, conta.Instituicao, conta.Ativa, conta.CriadaEm), ct);
    }

    public record FeatureRequest(Guid Id, string Nome);

    public record FeatureResponse(Guid Id, string Nome, TipoConta Tipo, string? Instituicao, bool Ativa, DateTime CriadaEm);
}

public class AtualizarValidator : Validator<Atualizar.FeatureRequest>
{
    public AtualizarValidator()
    {
        RuleFor(r => r.Nome)
            .NotEmpty().WithMessage("O nome da conta é obrigatório.")
            .MaximumLength(100).WithMessage("O nome deve ter no máximo 100 caracteres.");
    }
}
