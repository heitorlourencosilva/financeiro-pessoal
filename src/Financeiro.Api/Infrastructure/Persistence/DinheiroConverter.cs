using System.Globalization;
using Financeiro.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Financeiro.Api.Infrastructure.Persistence;

public class DinheiroConverter : ValueConverter<Dinheiro, string>
{
    public DinheiroConverter()
        : base(
            v => ConvertTo(v),
            s => ConvertFrom(s))
    {
    }

    private static string ConvertTo(Dinheiro v)
        => $"{v.Valor:N2};{v.Moeda}";

    private static Dinheiro ConvertFrom(string s)
    {
        var partes = s.Split(';');
        return new Dinheiro(decimal.Parse(partes[0], CultureInfo.InvariantCulture), partes[1]);
    }
}
