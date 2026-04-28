using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes;

[DbContext(typeof(PlataformaFutevoleiDbContext))]
[Migration("20260330201500_AdicionarStatusEnvioEmailConviteCadastro")]
public partial class AdicionarStatusEnvioEmailConviteCadastro : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "email_enviado_em_utc",
            table: "convites_cadastro",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "erro_envio_email",
            table: "convites_cadastro",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "ultima_tentativa_envio_email_em_utc",
            table: "convites_cadastro",
            type: "timestamp with time zone",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "email_enviado_em_utc",
            table: "convites_cadastro");

        migrationBuilder.DropColumn(
            name: "erro_envio_email",
            table: "convites_cadastro");

        migrationBuilder.DropColumn(
            name: "ultima_tentativa_envio_email_em_utc",
            table: "convites_cadastro");
    }
}
