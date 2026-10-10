namespace Financeiro.Domain.Entities;

public class Tag
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public DateTime CriadaEm { get; private set; }

    private Tag()
    {
        Nome = null!;
    }

    public Tag(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome da tag é obrigatório.", nameof(nome));

        Id = Guid.NewGuid();
        Nome = nome.Trim().ToLowerInvariant();
        CriadaEm = DateTime.UtcNow;
    }

    public void Renomear(string novoNome)
    {
        if (string.IsNullOrWhiteSpace(novoNome))
            throw new ArgumentException("Nome da tag é obrigatória.", nameof(novoNome));
        Nome = novoNome.Trim().ToLowerInvariant();
    }
}