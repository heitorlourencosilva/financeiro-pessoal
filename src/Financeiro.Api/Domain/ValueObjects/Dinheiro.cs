namespace Financeiro.Domain.ValueObjects;

public readonly record struct Dinheiro
{
    public decimal Valor { get; }
    public string Moeda { get; }

    public Dinheiro(decimal valor, string moeda = "BRL")
    {
        if (string.IsNullOrWhiteSpace(moeda))
            throw new ArgumentException("Moeda é obrigatória.", nameof(moeda));

        Valor = decimal.Round(valor, 2, MidpointRounding.AwayFromZero);
        Moeda = moeda.ToUpperInvariant();
    }

    public static Dinheiro Zero(string moeda = "BRL") => new(0m, moeda);

    public Dinheiro Abs() => new(Math.Abs(Valor), Moeda);

    public Dinheiro Somar(Dinheiro outro)
    {
        if (Moeda != outro.Moeda)
            throw new InvalidOperationException("Não é possível somar moedas diferentes.");
        return new(Valor + outro.Valor, Moeda);
    }

    public Dinheiro Negar() => new(-Valor, Moeda);

    public static Dinheiro operator +(Dinheiro a, Dinheiro b)
    {
        if (a.Moeda != b.Moeda)
            throw new InvalidOperationException("Não é possível somar moedas diferentes.");
        return new(a.Valor + b.Valor, a.Moeda);
    }

    public static Dinheiro operator -(Dinheiro a, Dinheiro b)
    {
        if (a.Moeda != b.Moeda)
            throw new InvalidOperationException("Não é possível subtrair moedas diferentes.");
        return new(a.Valor - b.Valor, a.Moeda);
    }

    public override string ToString() => $"{Moeda} {Valor:N2}";
}