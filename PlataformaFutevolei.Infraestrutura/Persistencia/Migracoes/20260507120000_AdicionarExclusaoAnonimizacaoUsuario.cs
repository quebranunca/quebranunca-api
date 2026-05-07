using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes;

[DbContext(typeof(PlataformaFutevoleiDbContext))]
[Migration("20260507120000_AdicionarExclusaoAnonimizacaoUsuario")]
public partial class AdicionarExclusaoAnonimizacaoUsuario : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "dados_anonimizados",
            table: "usuarios",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTime>(
            name: "excluido_em_utc",
            table: "usuarios",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "excluido_por_usuario_id",
            table: "usuarios",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_usuarios_excluido_por_usuario_id",
            table: "usuarios",
            column: "excluido_por_usuario_id");

        migrationBuilder.AddForeignKey(
            name: "fk_usuarios_usuarios_excluido_por_usuario_id",
            table: "usuarios",
            column: "excluido_por_usuario_id",
            principalTable: "usuarios",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_usuarios_usuarios_excluido_por_usuario_id",
            table: "usuarios");

        migrationBuilder.DropIndex(
            name: "ix_usuarios_excluido_por_usuario_id",
            table: "usuarios");

        migrationBuilder.DropColumn(
            name: "dados_anonimizados",
            table: "usuarios");

        migrationBuilder.DropColumn(
            name: "excluido_em_utc",
            table: "usuarios");

        migrationBuilder.DropColumn(
            name: "excluido_por_usuario_id",
            table: "usuarios");
    }
}
