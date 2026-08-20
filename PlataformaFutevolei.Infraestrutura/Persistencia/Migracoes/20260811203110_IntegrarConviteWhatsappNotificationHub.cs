using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class IntegrarConviteWhatsappNotificationHub : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "whatsapp_idempotency_key",
                table: "convites_cadastro",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "whatsapp_notification_hub_id",
                table: "convites_cadastro",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "whatsapp_idempotency_key",
                table: "convites_cadastro");

            migrationBuilder.DropColumn(
                name: "whatsapp_notification_hub_id",
                table: "convites_cadastro");
        }
    }
}
