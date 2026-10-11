using System.Globalization;
using Financeiro.Domain.Enums;
using Financeiro.Domain.ValueObjects;

namespace Financeiro.Infrastructure.Importers;

public sealed class LinhaImportada
{
    public required DateOnly Data { get; init; }
    public required string DescricaoOriginal { get; init; }
    public required Dinheiro Valor { get; init; }
    public required NaturezaTransacao Natureza { get; init; }
}

public sealed class ItauCsvParser
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public IReadOnlyList<LinhaImportada> Parse(TextReader reader)
    {
        var linhas = new List<LinhaImportada>();
        string? linha;
        var numeroLinha = 0;

        while ((linha = reader.ReadLine()) is not null)
        {
            numeroLinha++;

            if (string.IsNullOrWhiteSpace(linha))
                continue;

            var partes = linha.Split(';');
            if (partes.Length != 3)
                throw new FormatException(
                    $"Linha {numeroLinha}: esperado 3 colunas separadas por ';', encontrado {partes.Length}.");

            DateOnly data;
            try
            {
                data = DateOnly.ParseExact(partes[0].Trim(), "dd/MM/yyyy", PtBr);
            }
            catch (FormatException)
            {
                throw new FormatException(
                    $"Linha {numeroLinha}: data inválida '{partes[0].Trim()}'.");
            }

            var descricao = partes[1].Trim();
            if (string.IsNullOrWhiteSpace(descricao))
                throw new FormatException(
                    $"Linha {numeroLinha}: descrição vazia.");

            decimal valorBruto;
            try
            {
                valorBruto = decimal.Parse(partes[2].Trim(), NumberStyles.Number, PtBr);
            }
            catch (FormatException)
            {
                throw new FormatException(
                    $"Linha {numeroLinha}: valor inválido '{partes[2].Trim()}'.");
            }

            var natureza = valorBruto < 0
                ? NaturezaTransacao.Despesa
                : NaturezaTransacao.Receita;

            linhas.Add(new LinhaImportada
            {
                Data = data,
                DescricaoOriginal = descricao,
                Valor = new Dinheiro(Math.Abs(valorBruto), "BRL"),
                Natureza = natureza
            });
        }

        return linhas;
    }
}