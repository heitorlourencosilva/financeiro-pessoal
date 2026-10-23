namespace Financeiro.Features.Relatorios.Composicao;

public record ComposicaoResponse(
    IReadOnlyList<LinhaComposicao> Receitas,
    IReadOnlyList<LinhaComposicao> Despesas);

public record LinhaComposicao(
    Guid? CategoriaId,
    string CategoriaNome,
    decimal Total);
