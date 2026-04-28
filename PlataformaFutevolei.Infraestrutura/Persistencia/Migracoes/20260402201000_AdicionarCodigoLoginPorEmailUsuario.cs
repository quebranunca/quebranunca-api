using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes;

[DbContext(typeof(PlataformaFutevoleiDbContext))]
[Migration("20260402201000_AdicionarCodigoLoginPorEmailUsuario")]
public partial class AdicionarCodigoLoginPorEmailUsuario : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "codigo_login_expira_em_utc",
            table: "usuarios",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "codigo_login_hash",
            table: "usuarios",
            type: "character varying(255)",
            maxLength: 255,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "codigo_login_expira_em_utc",
            table: "usuarios");

        migrationBuilder.DropColumn(
            name: "codigo_login_hash",
            table: "usuarios");
    }
}
