using Financeiro.Domain.Enums;
using Financeiro.Domain.ValueObjects;
using Financeiro.Infrastructure.Persistence;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Features.Transacoes;

public class Atualizar : Endpoint<Atualizar.FeatureRequest, TransacaoResponse>
{
    private readonly FinanceiroDbContext _context;

    public Atualizar(FinanceiroDbContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Put("/api/transacoes/{Id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(FeatureRequest request, CancellationToken ct)
    {
        var transacao = await _context.Transacoes
            .Include(t => t.Categoria)
            .SingleOrDefaultAsync(t => t.Id == request.Id, ct);

        if (transacao is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        var conta = await _context.Contas
            .SingleOrDefaultAsync(c => c.Id == request.ContaId, ct);

        if (conta is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        transacao.Atualizar(
            request.Data,
            request.Descricao,
            new Dinheiro(request.Valor, transacao.Valor.Moeda),
            request.Natureza,
            conta,
            request.Contexto,
            request.Pessoa,
            request.Observacao);

        await _context.SaveChangesAsync(ct);

        await SendOkAsync(TransacaoResponse.FromTransacao(transacao), ct);
    }

    public record FeatureRequest(
        Guid Id,
        DateOnly Data,
        string Descricao,
        decimal Valor,
        NaturezaTransacao Natureza,
        Guid ContaId,
        Contexto Contexto,
        Pessoa Pessoa,
        string? Observacao);
}

public class AtualizarValidator : Validator<Atualizar.FeatureRequest>
{
    public AtualizarValidator()
    {
        RuleFor(r => r.Data)
            .NotEmpty().WithMessage("A data da transação é obrigatória.");

        RuleFor(r => r.Descricao)
            .NotEmpty().WithMessage("A descrição é obrigatória.")
            .MaximumLength(200).WithMessage("A descrição deve ter no máximo 200 caracteres.");

        RuleFor(r => r.Valor)
            .GreaterThan(0).WithMessage("O valor deve ser maior que zero.");

        RuleFor(r => r.Natureza)
            .IsInEnum().WithMessage("A natureza da transação é inválida.");

        RuleFor(r => r.ContaId)
            .NotEmpty().WithMessage("A conta é obrigatória.");
    }
}