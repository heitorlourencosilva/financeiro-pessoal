using Financeiro.Domain.Entities;
using Financeiro.Infrastructure.Persistence;
using Financeiro.Infrastructure.Importers;
using Financeiro.Features.Classificacao;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Features.Transacoes;

public class Importar : EndpointWithoutRequest<Importar.FeatureResponse>
{
    private const long TamanhoMaximoArquivo = 50L * 1024 * 1024;

    private readonly FinanceiroDbContext _context;
    private readonly ItauCsvParser _parser;
    private readonly ServicoClassificacao _servicoClassificacao;

    public Importar(
        FinanceiroDbContext context,
        ItauCsvParser parser,
        ServicoClassificacao servicoClassificacao)
    {
        _context = context;
        _parser = parser;
        _servicoClassificacao = servicoClassificacao;
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

        if (arquivo.Length > TamanhoMaximoArquivo)
        {
            AddError("arquivo", "O arquivo CSV não pode exceder 50 MiB.");
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

        if (!conta.Ativa)
        {
            AddError("contaId", "A conta selecionada está inativa.");
            await SendErrorsAsync(400, ct);
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

        var transacoes = linhas
            .Select(linha => new Transacao(
                data: linha.Data,
                descricaoOriginal: linha.DescricaoOriginal,
                valor: linha.Valor,
                natureza: linha.Natureza,
                conta: conta))
            .ToArray();

        await _servicoClassificacao.AplicarRegrasAutomaticasAsync(transacoes, ct);
        _context.Transacoes.AddRange(transacoes);

        await _context.SaveChangesAsync(ct);

        await SendOkAsync(new FeatureResponse(linhas.Count, Array.Empty<string>()), ct);
    }

    public record FeatureResponse(int Importadas, IReadOnlyList<string> Erros);
}