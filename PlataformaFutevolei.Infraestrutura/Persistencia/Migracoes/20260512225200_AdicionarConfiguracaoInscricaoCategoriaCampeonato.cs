using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes;

[DbContext(typeof(PlataformaFutevoleiDbContext))]
[Migration("20260512225200_AdicionarConfiguracaoInscricaoCategoriaCampeonato")]
public partial class AdicionarConfiguracaoInscricaoCategoriaCampeonato : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "status_campeonato",
            table: "competicoes",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "ativo",
            table: "categorias_competicao",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "data_abertura_inscricao",
            table: "categorias_competicao",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "data_encerramento_inscricao",
            table: "categorias_competicao",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "observacao",
            table: "categorias_competicao",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "permite_lista_espera",
            table: "categorias_competicao",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "status_inscricao",
            table: "categorias_competicao",
            type: "integer",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<decimal>(
            name: "valor_inscricao",
            table: "categorias_competicao",
            type: "numeric(10,2)",
            precision: 10,
            scale: 2,
            nullable: false,
            defaultValue: 0m);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "status_campeonato",
            table: "competicoes");

        migrationBuilder.DropColumn(
            name: "ativo",
            table: "categorias_competicao");

        migrationBuilder.DropColumn(
            name: "data_abertura_inscricao",
            table: "categorias_competicao");

        migrationBuilder.DropColumn(
            name: "data_encerramento_inscricao",
            table: "categorias_competicao");

        migrationBuilder.DropColumn(
            name: "observacao",
            table: "categorias_competicao");

        migrationBuilder.DropColumn(
            name: "permite_lista_espera",
            table: "categorias_competicao");

        migrationBuilder.DropColumn(
            name: "status_inscricao",
            table: "categorias_competicao");

        migrationBuilder.DropColumn(
            name: "valor_inscricao",
            table: "categorias_competicao");
    }
}
