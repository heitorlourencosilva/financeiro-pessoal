using Financeiro.Domain.Enums;
using Financeiro.Domain.ValueObjects;

namespace Financeiro.Domain.Entities;

public class Conta
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public TipoConta Tipo { get; private set; }
    public string? Instituicao { get; private set; }
    public bool Ativa { get; private set; }
    public DateTime CriadaEm { get; private set; }

    private Conta()
    {
        Nome = null!;
    }

    public Conta(string nome, TipoConta tipo, string? instituicao = null)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome da conta é obrigatório.", nameof(nome));

        Id = Guid.NewGuid();
        Nome = nome.Trim();
        Tipo = tipo;
        Instituicao = instituicao?.Trim();
        Ativa = true;
        CriadaEm = DateTime.UtcNow;
    }

    public void Desativar() => Ativa = false;
    public void Reativar() => Ativa = true;

    public void Renomear(string novoNome)
    {
        if (string.IsNullOrWhiteSpace(novoNome))
            throw new ArgumentException("Nome da conta é obrigatório.", nameof(novoNome));
        Nome = novoNome.Trim();
    }
}