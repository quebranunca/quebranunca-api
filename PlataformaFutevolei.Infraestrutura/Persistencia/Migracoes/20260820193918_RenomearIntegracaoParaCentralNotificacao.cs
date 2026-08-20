using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class RenomearIntegracaoParaCentralNotificacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "whatsapp_notification_hub_id",
                table: "convites_cadastro",
                newName: "whatsapp_central_notificacao_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "whatsapp_central_notificacao_id",
                table: "convites_cadastro",
                newName: "whatsapp_notification_hub_id");
        }
    }
}
