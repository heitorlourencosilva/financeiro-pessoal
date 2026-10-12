using Financeiro.Domain.Entities;
using Financeiro.Infrastructure.Persistence;
using Financeiro.Infrastructure.Importers;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Features.Transacoes;

public class Importar : EndpointWithoutRequest<Importar.FeatureResponse>
{
    private readonly FinanceiroDbContext _context;
    private readonly ItauCsvParser _parser;

    public Importar(FinanceiroDbContext context, ItauCsvParser parser)
    {
        _context = context;
        _parser = parser;
    }

    public override void Configure()
    {
        Post("/api/transacoes/importar");
        AllowAnonymous();
        AllowFileUploads();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var arquivo = Files.GetFile("arquivo");
        var contaIdRaw = Form["contaId"].ToString();

        if (arquivo is null || arquivo.Length == 0)
        {
            AddError("arquivo", "Arquivo CSV é obrigatório.");
            await SendErrorsAsync(400, ct);
            return;
        }

        if (!Guid.TryParse(contaIdRaw, out var contaId))
        {
            AddError("contaId", "Conta é obrigatória e deve ser um Guid válido.");
            await SendErrorsAsync(400, ct);
            return;
        }

        var conta = await _context.Contas
            .SingleOrDefaultAsync(c => c.Id == contaId, ct);

        if (conta is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await using var stream = arquivo.OpenReadStream();
        using var reader = new StreamReader(stream);

        IReadOnlyList<LinhaImportada> linhas;
        try
        {
            linhas = _parser.Parse(reader);
        }
        catch (FormatException ex)
        {
            AddError("arquivo", ex.Message);
            await SendErrorsAsync(400, ct);
            return;
        }

        foreach (var linha in linhas)
        {
            var transacao = new Transacao(
                data: linha.Data,
                descricaoOriginal: linha.DescricaoOriginal,
                valor: linha.Valor,
                natureza: linha.Natureza,
                conta: conta);

            _context.Transacoes.Add(transacao);
        }

        await _context.SaveChangesAsync(ct);

        await SendOkAsync(new FeatureResponse(linhas.Count, Array.Empty<string>()), ct);
    }

    public record FeatureResponse(int Importadas, IReadOnlyList<string> Erros);
}