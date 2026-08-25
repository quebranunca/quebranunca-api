using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class AdicionarFallbackSmsConvites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "erro_envio_sms",
                table: "convites_cadastro",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sms_entrega_id",
                table: "convites_cadastro",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "sms_enviado_em_utc",
                table: "convites_cadastro",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sms_idempotency_key",
                table: "convites_cadastro",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ultima_tentativa_envio_sms_em_utc",
                table: "convites_cadastro",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "erro_envio_sms",
                table: "convites_cadastro");

            migrationBuilder.DropColumn(
                name: "sms_entrega_id",
                table: "convites_cadastro");

            migrationBuilder.DropColumn(
                name: "sms_enviado_em_utc",
                table: "convites_cadastro");

            migrationBuilder.DropColumn(
                name: "sms_idempotency_key",
                table: "convites_cadastro");

            migrationBuilder.DropColumn(
                name: "ultima_tentativa_envio_sms_em_utc",
                table: "convites_cadastro");
        }
    }
}
