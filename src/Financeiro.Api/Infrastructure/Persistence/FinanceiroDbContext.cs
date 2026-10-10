using Financeiro.Domain.Entities;
using Financeiro.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Financeiro.Api.Infrastructure.Persistence;

public class FinanceiroDbContext : DbContext
{
    public FinanceiroDbContext(DbContextOptions<FinanceiroDbContext> options)
        : base(options) { }

    // --- DbSets ---

    public DbSet<Conta> Contas => Set<Conta>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Transacao> Transacoes => Set<Transacao>();
    public DbSet<Tag> Tags => Set<Tag>();

    // --- Configuração do modelo ---

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Conta ---
        modelBuilder.Entity<Conta>(builder =>
        {
            builder.ToTable("contas");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("id");

            builder.Property(e => e.Nome)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("nome");

            builder.Property(e => e.Tipo)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("tipo");

            builder.Property(e => e.Instituicao)
                .HasMaxLength(200)
                .HasColumnName("instituicao");

            builder.Property(e => e.Ativa)
                .IsRequired()
                .HasColumnName("ativa")
                .HasDefaultValue(true);

            builder.Property(e => e.CriadaEm)
                .IsRequired()
                .HasColumnName("criada_em");
        });

        // --- Categoria (self-referencing hierarchy) ---
        modelBuilder.Entity<Categoria>(builder =>
        {
            builder.ToTable("categorias");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("id");

            builder.Property(e => e.Nome)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("nome");

            builder.Property(e => e.CategoriaPaiId)
                .HasColumnName("categoria_pai_id");

            builder.Property(e => e.Ativa)
                .IsRequired()
                .HasColumnName("ativa")
                .HasDefaultValue(true);

            builder.Property(e => e.CriadaEm)
                .IsRequired()
                .HasColumnName("criada_em");

            builder.HasOne(e => e.CategoriaPai)
                .WithMany(e => e.Subcategorias)
                .HasForeignKey(e => e.CategoriaPaiId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- Tag ---
        modelBuilder.Entity<Tag>(builder =>
        {
            builder.ToTable("tags");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("id");

            builder.Property(e => e.Nome)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("nome");

            builder.HasIndex(e => e.Nome).IsUnique();

            builder.Property(e => e.CriadaEm)
                .IsRequired()
                .HasColumnName("criada_em");
        });

        // --- Transacao ---
        modelBuilder.Entity<Transacao>(builder =>
        {
            builder.ToTable("transacoes");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("id");

            // --- "Quando" ---
            builder.Property(e => e.Data)
                .IsRequired()
                .HasColumnName("data");

            // --- "O quê" ---
            builder.Property(e => e.DescricaoOriginal)
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnName("descricao_original");

            builder.Property(e => e.DescricaoNormalizada)
                .HasMaxLength(500)
                .HasColumnName("descricao_normalizada");

            // Dinheiro usando value converter (structs não são suportados como owned types no EF Core 10)
            // Armazena como "valor;moeda" (ex: "1500,50;BRL") para facilitar parsing
            builder.Property(e => e.Valor)
                .HasConversion<DinheiroConverter>()
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("valor");

            builder.Property(e => e.Natureza)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("natureza");

            // --- "Onde" ---
            builder.Property(e => e.ContaId)
                .IsRequired()
                .HasColumnName("conta_id");

            builder.HasOne(e => e.Conta)
                .WithMany()
                .HasForeignKey(e => e.ContaId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- Classificação ---
            builder.Property(e => e.CategoriaId)
                .HasColumnName("categoria_id");

            builder.HasOne(e => e.Categoria)
                .WithMany()
                .HasForeignKey(e => e.CategoriaId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Property(e => e.Contexto)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("contexto");

            builder.Property(e => e.Pessoa)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("pessoa");

            // Tags — many-to-many implícita via EF Core 10
            builder.HasMany(e => e.Tags)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "TransacaoTags",
                    j => j.HasOne<Tag>()
                        .WithMany()
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.Restrict),
                    j => j.HasOne<Transacao>()
                        .WithMany()
                        .HasForeignKey("TransacaoId")
                        .OnDelete(DeleteBehavior.Restrict),
                    j =>
                    {
                        j.ToTable("transacao_tags");
                        j.HasKey("TransacaoId", "TagId");
                        j.Property("TransacaoId").HasColumnName("transacao_id");
                        j.Property("TagId").HasColumnName("tag_id");
                    });

            // --- Controle ---
            builder.Property(e => e.Classificada)
                .IsRequired()
                .HasColumnName("classificada")
                .HasDefaultValue(false);

            builder.Property(e => e.Observacao)
                .HasMaxLength(1000)
                .HasColumnName("observacao");

            builder.Property(e => e.ImportadaEm)
                .IsRequired()
                .HasColumnName("importada_em");

            builder.Property(e => e.ClassificadaEm)
                .HasColumnName("classificada_em");
        });
    }
}