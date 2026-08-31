using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class AdicionarAgendaEConfirmacaoPresencaGrupos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "horario_fim",
                table: "grupos",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "horario_inicio",
                table: "grupos",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "encontros_grupos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    grupo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    data_jogo = table.Column<DateOnly>(type: "date", nullable: false),
                    horario_inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    horario_fim = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    arena_id = table.Column<Guid>(type: "uuid", nullable: true),
                    local_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_encontros_grupos", x => x.id);
                    table.ForeignKey(
                        name: "FK_encontros_grupos_arenas_arena_id",
                        column: x => x.arena_id,
                        principalTable: "arenas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_encontros_grupos_grupos_grupo_id",
                        column: x => x.grupo_id,
                        principalTable: "grupos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "confirmacoes_presenca_grupos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    encontro_grupo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    atleta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo_acesso = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    expira_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    respondida_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tentativas_envio_whatsapp = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ultima_tentativa_envio_whatsapp_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    whatsapp_enviado_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    whatsapp_mensagem_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    erro_envio_whatsapp = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_confirmacoes_presenca_grupos", x => x.id);
                    table.ForeignKey(
                        name: "FK_confirmacoes_presenca_grupos_atletas_atleta_id",
                        column: x => x.atleta_id,
                        principalTable: "atletas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_confirmacoes_presenca_grupos_encontros_grupos_encontro_grup~",
                        column: x => x.encontro_grupo_id,
                        principalTable: "encontros_grupos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_confirmacoes_presenca_grupos_atleta_id",
                table: "confirmacoes_presenca_grupos",
                column: "atleta_id");

            migrationBuilder.CreateIndex(
                name: "IX_confirmacoes_presenca_grupos_codigo_acesso",
                table: "confirmacoes_presenca_grupos",
                column: "codigo_acesso",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_confirmacoes_presenca_grupos_encontro_grupo_id_atleta_id",
                table: "confirmacoes_presenca_grupos",
                columns: new[] { "encontro_grupo_id", "atleta_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_confirmacoes_presenca_grupos_encontro_grupo_id_status",
                table: "confirmacoes_presenca_grupos",
                columns: new[] { "encontro_grupo_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_encontros_grupos_arena_id",
                table: "encontros_grupos",
                column: "arena_id");

            migrationBuilder.CreateIndex(
                name: "IX_encontros_grupos_data_jogo",
                table: "encontros_grupos",
                column: "data_jogo");

            migrationBuilder.CreateIndex(
                name: "IX_encontros_grupos_grupo_id_data_jogo",
                table: "encontros_grupos",
                columns: new[] { "grupo_id", "data_jogo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "confirmacoes_presenca_grupos");

            migrationBuilder.DropTable(
                name: "encontros_grupos");

            migrationBuilder.DropColumn(
                name: "horario_fim",
                table: "grupos");

            migrationBuilder.DropColumn(
                name: "horario_inicio",
                table: "grupos");
        }
    }
}
