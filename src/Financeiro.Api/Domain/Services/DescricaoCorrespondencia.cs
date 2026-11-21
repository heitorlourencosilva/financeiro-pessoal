using System.Text.RegularExpressions;

namespace Financeiro.Domain.Services;

public static partial class DescricaoCorrespondencia
{
    public static bool ContemTermo(string descricaoNormalizada, string termoNormalizado)
    {
        if (string.IsNullOrWhiteSpace(descricaoNormalizada) ||
            string.IsNullOrWhiteSpace(termoNormalizado))
            return false;

        var inicio = 0;
        while (inicio < descricaoNormalizada.Length)
        {
            var indice = descricaoNormalizada.IndexOf(termoNormalizado, inicio, StringComparison.Ordinal);
            if (indice < 0)
                return false;

            var fim = indice + termoNormalizado.Length;
            var limiteEsquerdo = indice == 0 || !char.IsLetterOrDigit(descricaoNormalizada[indice - 1]);
            var limiteDireito = fim == descricaoNormalizada.Length || !char.IsLetterOrDigit(descricaoNormalizada[fim]);
            if (limiteEsquerdo && limiteDireito)
                return true;

            inicio = indice + 1;
        }

        return false;
    }

    public static double Similaridade(string descricaoNormalizada, string outraDescricaoNormalizada)
    {
        var termos = Termos(descricaoNormalizada);
        var outrosTermos = Termos(outraDescricaoNormalizada);
        if (termos.Count == 0 || outrosTermos.Count == 0)
            return 0d;

        var intersecao = termos.Intersect(outrosTermos, StringComparer.Ordinal).Count();
        if (intersecao < 2)
            return 0d;

        var similaridade = (double)intersecao / Math.Max(termos.Count, outrosTermos.Count);
        return similaridade >= 0.6d ? similaridade : 0d;
    }

    private static HashSet<string> Termos(string descricao) =>
        TermoRegex()
            .Matches(descricao)
            .Select(m => m.Value)
            .ToHashSet(StringComparer.Ordinal);

    [GeneratedRegex(@"[\p{L}\p{N}]+")]
    private static partial Regex TermoRegex();
}
