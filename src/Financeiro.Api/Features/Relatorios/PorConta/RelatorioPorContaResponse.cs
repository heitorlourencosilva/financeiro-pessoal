namespace Financeiro.Features.Relatorios.PorConta;

public record RelatorioPorContaResponse(
    IReadOnlyList<LinhaRelatorioConta> Linhas,
    LinhaRelatorioConta Total);

public record LinhaRelatorioConta(
    Guid ContaId,
    string ContaNome,
    decimal TotalReceitas,
    decimal TotalDespesas,
    decimal TotalTransferencias,
    decimal TotalInvestimentos,
    decimal SaldoPeriodo)
{
    public static LinhaRelatorioConta Vazia(Guid contaId, string contaNome) =>
        new(contaId, contaNome, 0m, 0m, 0m, 0m, 0m);
}