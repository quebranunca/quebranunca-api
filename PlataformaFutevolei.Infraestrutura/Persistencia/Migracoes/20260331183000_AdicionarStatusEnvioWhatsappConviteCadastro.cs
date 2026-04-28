using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes;

[DbContext(typeof(PlataformaFutevoleiDbContext))]
[Migration("20260331183000_AdicionarStatusEnvioWhatsappConviteCadastro")]
public partial class AdicionarStatusEnvioWhatsappConviteCadastro : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "erro_envio_whatsapp",
            table: "convites_cadastro",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "ultima_tentativa_envio_whatsapp_em_utc",
            table: "convites_cadastro",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "whatsapp_enviado_em_utc",
            table: "convites_cadastro",
            type: "timestamp with time zone",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "erro_envio_whatsapp",
            table: "convites_cadastro");

        migrationBuilder.DropColumn(
            name: "ultima_tentativa_envio_whatsapp_em_utc",
            table: "convites_cadastro");

        migrationBuilder.DropColumn(
            name: "whatsapp_enviado_em_utc",
            table: "convites_cadastro");
    }
}
