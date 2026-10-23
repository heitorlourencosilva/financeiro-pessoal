namespace Financeiro.Features.Relatorios.Comparacao;

public record ComparacaoResponse(
    PeriodoResumo PeriodoA,
    PeriodoResumo PeriodoB,
    IReadOnlyList<LinhaComparacao> Receitas,
    IReadOnlyList<LinhaComparacao> Despesas);

public record PeriodoResumo(
    DateOnly Inicio,
    DateOnly Fim,
    decimal TotalReceitas,
    decimal TotalDespesas,
    decimal Saldo);

public record LinhaComparacao(
    Guid? CategoriaId,
    string CategoriaNome,
    decimal TotalA,
    decimal TotalB,
    decimal Diferenca,
    decimal? VariacaoPercentual);