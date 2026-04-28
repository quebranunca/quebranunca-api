using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes;

[DbContext(typeof(PlataformaFutevoleiDbContext))]
[Migration("20260325170000_AdicionarUsuarioCriadorEmLocaisERegras")]
public partial class AdicionarUsuarioCriadorEmLocaisERegras : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "usuario_criador_id",
            table: "regras_competicao",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "usuario_criador_id",
            table: "locais",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_regras_competicao_usuario_criador_id",
            table: "regras_competicao",
            column: "usuario_criador_id");

        migrationBuilder.CreateIndex(
            name: "IX_locais_usuario_criador_id",
            table: "locais",
            column: "usuario_criador_id");

        migrationBuilder.AddForeignKey(
            name: "FK_locais_usuarios_usuario_criador_id",
            table: "locais",
            column: "usuario_criador_id",
            principalTable: "usuarios",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "FK_regras_competicao_usuarios_usuario_criador_id",
            table: "regras_competicao",
            column: "usuario_criador_id",
            principalTable: "usuarios",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_locais_usuarios_usuario_criador_id",
            table: "locais");

        migrationBuilder.DropForeignKey(
            name: "FK_regras_competicao_usuarios_usuario_criador_id",
            table: "regras_competicao");

        migrationBuilder.DropIndex(
            name: "IX_regras_competicao_usuario_criador_id",
            table: "regras_competicao");

        migrationBuilder.DropIndex(
            name: "IX_locais_usuario_criador_id",
            table: "locais");

        migrationBuilder.DropColumn(
            name: "usuario_criador_id",
            table: "regras_competicao");

        migrationBuilder.DropColumn(
            name: "usuario_criador_id",
            table: "locais");
    }
}
