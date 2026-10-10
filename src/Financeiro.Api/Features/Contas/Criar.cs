using Financeiro.Domain.Entities;
using Financeiro.Domain.Enums;
using Financeiro.Api.Infrastructure.Persistence;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using FluentValidation;

namespace Financeiro.Features.Contas;

public class Criar : Endpoint<Criar.Request, Criar.Response>
{
    private readonly FinanceiroDbContext _context;

    public Criar(FinanceiroDbContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Post("/api/contas");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Criar.Request request, CancellationToken ct)
    {
        var conta = new Conta(
            nome: request.Nome,
            tipo: request.Tipo,
            instituicao: request.Instituicao);

        _context.Contas.Add(conta);
        await _context.SaveChangesAsync(ct);

        await SendOkAsync(new Response(conta.Id, conta.Nome, conta.Tipo, conta.Instituicao, conta.Ativa, conta.CriadaEm), ct);
    }

    public record Request(string Nome, TipoConta Tipo, string? Instituicao);

    public record Response(Guid Id, string Nome, TipoConta Tipo, string? Instituicao, bool Ativa, DateTime CriadaEm);
}

public class CriarValidator : Validator<Criar.Request>
{
    public CriarValidator()
    {
        RuleFor(r => r.Nome)
            .NotEmpty().WithMessage("O nome da conta é obrigatório.")
            .MaximumLength(100).WithMessage("O nome deve ter no máximo 100 caracteres.");

        RuleFor(r => r.Tipo)
            .IsInEnum().WithMessage("O tipo de conta é inválido.");

        RuleFor(r => r.Instituicao)
            .MaximumLength(100).WithMessage("A instituição deve ter no máximo 100 caracteres.");
    }
}
