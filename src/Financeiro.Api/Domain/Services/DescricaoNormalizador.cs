using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Financeiro.Domain.Services;

public static partial class DescricaoNormalizador
{
    public static string Normalizar(string descricao)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("Descrição é obrigatória.", nameof(descricao));

        var texto = descricao.Trim();

        texto = EspacosMultiplos().Replace(texto, " ");
        texto = RemoverAcentos(texto);
        texto = texto.ToUpperInvariant();
        texto = NumeroFinal().Replace(texto, "");
        texto = EspacosMultiplos().Replace(texto, " ").Trim();

        return texto;
    }

    private static string RemoverAcentos(string texto)
    {
        var normalizado = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalizado.Length);

        foreach (var c in normalizado)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex EspacosMultiplos();

    [GeneratedRegex(@"\s+\d{6,}$")]
    private static partial Regex NumeroFinal();
}