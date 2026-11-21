using System;
using Financeiro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Financeiro.Infrastructure.Persistence.Migrations;

[DbContext(typeof(FinanceiroDbContext))]
[Migration("20261120235800_RegrasClassificacaoAutomatica")]
public partial class RegrasClassificacaoAutomatica : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "regras_classificacao_automatica",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                descricao_correspondencia_normalizada = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                categoria_id = table.Column<Guid>(type: "uuid", nullable: false),
                contexto = table.Column<int>(type: "integer", nullable: false),
                pessoa = table.Column<int>(type: "integer", nullable: false),
                prioridade = table.Column<int>(type: "integer", nullable: false),
                ativa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                atualizada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_regras_classificacao_automatica", x => x.id);
                table.ForeignKey(
                    name: "FK_regras_classificacao_automatica_categorias_categoria_id",
                    column: x => x.categoria_id,
                    principalTable: "categorias",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_regras_classificacao_automatica_ativa_prioridade",
            table: "regras_classificacao_automatica",
            columns: new[] { "ativa", "prioridade" });

        migrationBuilder.CreateIndex(
            name: "IX_regras_classificacao_automatica_categoria_id",
            table: "regras_classificacao_automatica",
            column: "categoria_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "regras_classificacao_automatica");
    }
}
