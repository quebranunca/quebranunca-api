using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes;

[DbContext(typeof(PlataformaFutevoleiDbContext))]
[Migration("20260511150000_AdicionarUsuarioCriadorAtleta")]
public partial class AdicionarUsuarioCriadorAtleta : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "usuario_criador_id",
            table: "atletas",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_atletas_usuario_criador_id",
            table: "atletas",
            column: "usuario_criador_id");

        migrationBuilder.AddForeignKey(
            name: "fk_atletas_usuarios_usuario_criador_id",
            table: "atletas",
            column: "usuario_criador_id",
            principalTable: "usuarios",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_atletas_usuarios_usuario_criador_id",
            table: "atletas");

        migrationBuilder.DropIndex(
            name: "ix_atletas_usuario_criador_id",
            table: "atletas");

        migrationBuilder.DropColumn(
            name: "usuario_criador_id",
            table: "atletas");
    }
}
