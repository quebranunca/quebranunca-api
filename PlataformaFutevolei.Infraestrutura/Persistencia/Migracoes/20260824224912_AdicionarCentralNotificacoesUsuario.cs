using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class AdicionarCentralNotificacoesUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notificacoes_usuarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    origem = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    chave_idempotencia = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tipo = table.Column<int>(type: "integer", nullable: false),
                    prioridade = table.Column<int>(type: "integer", nullable: false),
                    titulo = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    mensagem = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    link_acao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    texto_acao = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    referencia_tipo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    referencia_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    lida_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    arquivada_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notificacoes_usuarios", x => x.id);
                    table.ForeignKey(
                        name: "FK_notificacoes_usuarios_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notificacoes_usuarios_usuario_id_lida_em_utc_arquivada_em_u~",
                table: "notificacoes_usuarios",
                columns: new[] { "usuario_id", "lida_em_utc", "arquivada_em_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_notificacoes_usuarios_usuario_id_origem_chave_idempotencia",
                table: "notificacoes_usuarios",
                columns: new[] { "usuario_id", "origem", "chave_idempotencia" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notificacoes_usuarios");
        }
    }
}
