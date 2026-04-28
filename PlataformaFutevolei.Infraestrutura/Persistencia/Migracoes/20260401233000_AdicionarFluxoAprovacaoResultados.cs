using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    [Migration("20260401233000_AdicionarFluxoAprovacaoResultados")]
    public partial class AdicionarFluxoAprovacaoResultados : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "status_aprovacao",
                table: "partidas",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.CreateTable(
                name: "partidas_aprovacoes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    partida_id = table.Column<Guid>(type: "uuid", nullable: false),
                    atleta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    data_solicitacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_resposta = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    observacao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partidas_aprovacoes", x => x.id);
                    table.ForeignKey(
                        name: "FK_partidas_aprovacoes_atletas_atleta_id",
                        column: x => x.atleta_id,
                        principalTable: "atletas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_partidas_aprovacoes_partidas_partida_id",
                        column: x => x.partida_id,
                        principalTable: "partidas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_partidas_aprovacoes_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pendencias_usuarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<int>(type: "integer", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    atleta_id = table.Column<Guid>(type: "uuid", nullable: true),
                    partida_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    data_conclusao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    observacao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pendencias_usuarios", x => x.id);
                    table.ForeignKey(
                        name: "FK_pendencias_usuarios_atletas_atleta_id",
                        column: x => x.atleta_id,
                        principalTable: "atletas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_pendencias_usuarios_partidas_partida_id",
                        column: x => x.partida_id,
                        principalTable: "partidas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_pendencias_usuarios_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_partidas_status_aprovacao",
                table: "partidas",
                column: "status_aprovacao");

            migrationBuilder.CreateIndex(
                name: "IX_partidas_aprovacoes_atleta_id",
                table: "partidas_aprovacoes",
                column: "atleta_id");

            migrationBuilder.CreateIndex(
                name: "IX_partidas_aprovacoes_partida_id",
                table: "partidas_aprovacoes",
                column: "partida_id");

            migrationBuilder.CreateIndex(
                name: "IX_partidas_aprovacoes_usuario_id",
                table: "partidas_aprovacoes",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_partidas_aprovacoes_partida_id_atleta_id",
                table: "partidas_aprovacoes",
                columns: new[] { "partida_id", "atleta_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pendencias_usuarios_atleta_id",
                table: "pendencias_usuarios",
                column: "atleta_id");

            migrationBuilder.CreateIndex(
                name: "IX_pendencias_usuarios_partida_id",
                table: "pendencias_usuarios",
                column: "partida_id");

            migrationBuilder.CreateIndex(
                name: "IX_pendencias_usuarios_usuario_id",
                table: "pendencias_usuarios",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_pendencias_usuarios_usuario_id_status",
                table: "pendencias_usuarios",
                columns: new[] { "usuario_id", "status" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "partidas_aprovacoes");

            migrationBuilder.DropTable(
                name: "pendencias_usuarios");

            migrationBuilder.DropIndex(
                name: "IX_partidas_status_aprovacao",
                table: "partidas");

            migrationBuilder.DropColumn(
                name: "status_aprovacao",
                table: "partidas");
        }
    }
}
