using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes;

[DbContext(typeof(PlataformaFutevoleiDbContext))]
[Migration("20260515120000_AdicionarLocalizacaoPartida")]
public partial class AdicionarLocalizacaoPartida : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<double>(
            name: "latitude",
            table: "partidas",
            type: "double precision",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "localizacao_registrada_em_utc",
            table: "partidas",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<double>(
            name: "longitude",
            table: "partidas",
            type: "double precision",
            nullable: true);

        migrationBuilder.AddColumn<double>(
            name: "precisao_localizacao",
            table: "partidas",
            type: "double precision",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "latitude",
            table: "partidas");

        migrationBuilder.DropColumn(
            name: "localizacao_registrada_em_utc",
            table: "partidas");

        migrationBuilder.DropColumn(
            name: "longitude",
            table: "partidas");

        migrationBuilder.DropColumn(
            name: "precisao_localizacao",
            table: "partidas");
    }
}
