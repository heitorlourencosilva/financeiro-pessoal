namespace Financeiro.Domain.ValueObjects;

public readonly record struct Periodo
{
    public DateOnly Inicio { get; }
    public DateOnly Fim { get; }

    public Periodo(DateOnly inicio, DateOnly fim)
    {
        if (fim < inicio)
            throw new ArgumentException("Fim não pode ser anterior ao início.");

        Inicio = inicio;
        Fim = fim;
    }

    public bool Contem(DateOnly data) => data >= Inicio && data <= Fim;

    public static Periodo MesAtual(DateOnly hoje)
    {
        var inicio = new DateOnly(hoje.Year, hoje.Month, 1);
        var fim = inicio.AddMonths(1).AddDays(-1);
        return new Periodo(inicio, fim);
    }

    public static Periodo MesAnterior(DateOnly hoje)
    {
        var primeiroDiaMesAtual = new DateOnly(hoje.Year, hoje.Month, 1);
        var fim = primeiroDiaMesAtual.AddDays(-1);
        var inicio = new DateOnly(fim.Year, fim.Month, 1);
        return new Periodo(inicio, fim);
    }

    public static Periodo MesCompleto(int ano, int mes)
    {
        var inicio = new DateOnly(ano, mes, 1);
        var fim = inicio.AddMonths(1).AddDays(-1);
        return new Periodo(inicio, fim);
    }
}