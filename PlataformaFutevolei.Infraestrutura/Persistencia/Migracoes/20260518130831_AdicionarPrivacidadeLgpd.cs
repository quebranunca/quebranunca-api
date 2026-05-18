using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class AdicionarPrivacidadeLgpd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "exclusao_solicitada_em_utc",
                table: "usuarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "exibir_email",
                table: "usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "perfil_publico",
                table: "usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "permitir_uso_imagem",
                table: "usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "permitir_uso_localizacao",
                table: "usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "usuarios_consentimentos_lgpd",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    versao_politica_privacidade = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    aceitou_politica_privacidade = table.Column<bool>(type: "boolean", nullable: false),
                    aceitou_termos_uso = table.Column<bool>(type: "boolean", nullable: false),
                    aceitou_uso_localizacao = table.Column<bool>(type: "boolean", nullable: false),
                    aceitou_uso_imagem = table.Column<bool>(type: "boolean", nullable: false),
                    aceito_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios_consentimentos_lgpd", x => x.id);
                    table.ForeignKey(
                        name: "FK_usuarios_consentimentos_lgpd_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_consentimentos_lgpd_usuario_id_versao_politica_pri~",
                table: "usuarios_consentimentos_lgpd",
                columns: new[] { "usuario_id", "versao_politica_privacidade" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "usuarios_consentimentos_lgpd");

            migrationBuilder.DropColumn(
                name: "exclusao_solicitada_em_utc",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "exibir_email",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "perfil_publico",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "permitir_uso_imagem",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "permitir_uso_localizacao",
                table: "usuarios");
        }
    }
}
