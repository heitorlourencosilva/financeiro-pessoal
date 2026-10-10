namespace Financeiro.Domain.Entities;

public class Categoria
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public Guid? CategoriaPaiId { get; private set; }
    public Categoria? CategoriaPai { get; private set; }
    public ICollection<Categoria> Subcategorias { get; private set; }
    public bool Ativa { get; private set; }
    public DateTime CriadaEm { get; private set; }

    private Categoria()
    {
        Nome = null!;
        Subcategorias = new List<Categoria>();
    }

    public Categoria(string nome, Categoria? pai = null)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome da categoria é obrigatório.", nameof(nome));

        Id = Guid.NewGuid();
        Nome = nome.Trim();
        CategoriaPai = pai;
        CategoriaPaiId = pai?.Id;
        Ativa = true;
        CriadaEm = DateTime.UtcNow;
        Subcategorias = new List<Categoria>();
    }

    public bool EhRaiz() => CategoriaPaiId is null;

    public void Desativar() => Ativa = false;
    public void Reativar() => Ativa = true;

    public void Renomear(string novoNome)
    {
        if (string.IsNullOrWhiteSpace(novoNome))
            throw new ArgumentException("Nome da categoria é obrigatório.", nameof(novoNome));
        Nome = novoNome.Trim();
    }
}