using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class AdicionarVinculoIdentityHub : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "usuarios_identidades_externas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    emissor = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    vinculada_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ultimo_login_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios_identidades_externas", x => x.id);
                    table.ForeignKey(
                        name: "FK_usuarios_identidades_externas_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_identidades_externas_emissor_subject",
                table: "usuarios_identidades_externas",
                columns: new[] { "emissor", "subject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_identidades_externas_usuario_id_emissor",
                table: "usuarios_identidades_externas",
                columns: new[] { "usuario_id", "emissor" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "usuarios_identidades_externas");
        }
    }
}
