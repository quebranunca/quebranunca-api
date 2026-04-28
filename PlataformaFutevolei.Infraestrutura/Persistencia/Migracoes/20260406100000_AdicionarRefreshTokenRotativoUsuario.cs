using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes;

[DbContext(typeof(PlataformaFutevoleiDbContext))]
[Migration("20260406100000_AdicionarRefreshTokenRotativoUsuario")]
public partial class AdicionarRefreshTokenRotativoUsuario : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "refresh_token_expira_em_utc",
            table: "usuarios",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "refresh_token_hash",
            table: "usuarios",
            type: "character varying(255)",
            maxLength: 255,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "refresh_token_expira_em_utc",
            table: "usuarios");

        migrationBuilder.DropColumn(
            name: "refresh_token_hash",
            table: "usuarios");
    }
}
