using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class DesacoplarCentralNotificacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "whatsapp_central_notificacao_id",
                table: "convites_cadastro",
                newName: "whatsapp_entrega_id");

            migrationBuilder.AlterColumn<string>(
                name: "whatsapp_entrega_id",
                table: "convites_cadastro",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "whatsapp_entrega_id",
                table: "convites_cadastro",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.RenameColumn(
                name: "whatsapp_entrega_id",
                table: "convites_cadastro",
                newName: "whatsapp_central_notificacao_id");
        }
    }
}
