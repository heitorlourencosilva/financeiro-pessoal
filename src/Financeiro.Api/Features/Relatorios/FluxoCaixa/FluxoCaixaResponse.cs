namespace Financeiro.Features.Relatorios.FluxoCaixa;

public record FluxoCaixaResponse(
    DateOnly DataInicio,
    DateOnly DataFim,
    decimal SaldoInicial,
    decimal TotalEntradas,
    decimal TotalSaidas,
    decimal SaldoFinal,
    IReadOnlyList<LinhaFluxoDiario> Diario);

public record LinhaFluxoDiario(
    DateOnly Data,
    decimal Entradas,
    decimal Saidas,
    decimal SaldoAcumulado);