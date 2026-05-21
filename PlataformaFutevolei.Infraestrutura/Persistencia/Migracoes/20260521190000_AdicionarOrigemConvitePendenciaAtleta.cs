using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes;

[DbContext(typeof(PlataformaFutevoleiDbContext))]
[Migration("20260521190000_AdicionarOrigemConvitePendenciaAtleta")]
public partial class AdicionarOrigemConvitePendenciaAtleta : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "atleta_id",
            table: "convites_cadastro",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "partida_id",
            table: "convites_cadastro",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_convites_cadastro_atleta_id",
            table: "convites_cadastro",
            column: "atleta_id");

        migrationBuilder.CreateIndex(
            name: "IX_convites_cadastro_partida_id",
            table: "convites_cadastro",
            column: "partida_id");

        migrationBuilder.AddForeignKey(
            name: "FK_convites_cadastro_atletas_atleta_id",
            table: "convites_cadastro",
            column: "atleta_id",
            principalTable: "atletas",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "FK_convites_cadastro_partidas_partida_id",
            table: "convites_cadastro",
            column: "partida_id",
            principalTable: "partidas",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_convites_cadastro_atletas_atleta_id",
            table: "convites_cadastro");

        migrationBuilder.DropForeignKey(
            name: "FK_convites_cadastro_partidas_partida_id",
            table: "convites_cadastro");

        migrationBuilder.DropIndex(
            name: "IX_convites_cadastro_atleta_id",
            table: "convites_cadastro");

        migrationBuilder.DropIndex(
            name: "IX_convites_cadastro_partida_id",
            table: "convites_cadastro");

        migrationBuilder.DropColumn(
            name: "atleta_id",
            table: "convites_cadastro");

        migrationBuilder.DropColumn(
            name: "partida_id",
            table: "convites_cadastro");
    }
}
