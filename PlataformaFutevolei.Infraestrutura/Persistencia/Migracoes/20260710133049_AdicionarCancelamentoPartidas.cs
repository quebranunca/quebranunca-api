using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class AdicionarCancelamentoPartidas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "solicitacao_cancelamento_partida_id",
                table: "pendencias_usuarios",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "cancelada",
                table: "partidas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "cancelada_em",
                table: "partidas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "excluida_definitivamente_em",
                table: "partidas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "excluida_definitivamente_por_usuario_id",
                table: "partidas",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "motivo_exclusao_definitiva",
                table: "partidas",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "solicitacao_cancelamento_origem_id",
                table: "partidas",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "solicitacoes_cancelamento_partidas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    partida_id = table.Column<Guid>(type: "uuid", nullable: false),
                    solicitada_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    solicitada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    dupla_solicitante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dupla_adversaria_id = table.Column<Guid>(type: "uuid", nullable: false),
                    motivo = table.Column<int>(type: "integer", nullable: false),
                    observacao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    respondida_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    respondida_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelada_pelo_solicitante_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_solicitacoes_cancelamento_partidas", x => x.id);
                    table.ForeignKey(
                        name: "FK_solicitacoes_cancelamento_partidas_duplas_dupla_adversaria_~",
                        column: x => x.dupla_adversaria_id,
                        principalTable: "duplas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_solicitacoes_cancelamento_partidas_duplas_dupla_solicitante~",
                        column: x => x.dupla_solicitante_id,
                        principalTable: "duplas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_solicitacoes_cancelamento_partidas_partidas_partida_id",
                        column: x => x.partida_id,
                        principalTable: "partidas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_solicitacoes_cancelamento_partidas_usuarios_respondida_por_~",
                        column: x => x.respondida_por_usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_solicitacoes_cancelamento_partidas_usuarios_solicitada_por_~",
                        column: x => x.solicitada_por_usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_pendencias_usuarios_cancelamento_atleta",
                table: "pendencias_usuarios",
                columns: new[] { "solicitacao_cancelamento_partida_id", "atleta_id" },
                unique: true,
                filter: "solicitacao_cancelamento_partida_id IS NOT NULL AND atleta_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_pendencias_usuarios_solicitacao_cancelamento_partida_id",
                table: "pendencias_usuarios",
                column: "solicitacao_cancelamento_partida_id");

            migrationBuilder.CreateIndex(
                name: "IX_partidas_cancelada",
                table: "partidas",
                column: "cancelada");

            migrationBuilder.CreateIndex(
                name: "IX_partidas_excluida_definitivamente_por_usuario_id",
                table: "partidas",
                column: "excluida_definitivamente_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_partidas_solicitacao_cancelamento_origem_id",
                table: "partidas",
                column: "solicitacao_cancelamento_origem_id");

            migrationBuilder.CreateIndex(
                name: "IX_solicitacoes_cancelamento_partidas_dupla_adversaria_id",
                table: "solicitacoes_cancelamento_partidas",
                column: "dupla_adversaria_id");

            migrationBuilder.CreateIndex(
                name: "IX_solicitacoes_cancelamento_partidas_dupla_solicitante_id",
                table: "solicitacoes_cancelamento_partidas",
                column: "dupla_solicitante_id");

            migrationBuilder.CreateIndex(
                name: "ix_solicitacoes_cancelamento_partidas_partida_pendente",
                table: "solicitacoes_cancelamento_partidas",
                column: "partida_id",
                unique: true,
                filter: "status = 1");

            migrationBuilder.CreateIndex(
                name: "IX_solicitacoes_cancelamento_partidas_respondida_por_usuario_id",
                table: "solicitacoes_cancelamento_partidas",
                column: "respondida_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_solicitacoes_cancelamento_partidas_solicitada_por_usuario_id",
                table: "solicitacoes_cancelamento_partidas",
                column: "solicitada_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_solicitacoes_cancelamento_partidas_status",
                table: "solicitacoes_cancelamento_partidas",
                column: "status");

            migrationBuilder.AddForeignKey(
                name: "FK_partidas_usuarios_excluida_definitivamente_por_usuario_id",
                table: "partidas",
                column: "excluida_definitivamente_por_usuario_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_pendencias_usuarios_solicitacoes_cancelamento_partidas_soli~",
                table: "pendencias_usuarios",
                column: "solicitacao_cancelamento_partida_id",
                principalTable: "solicitacoes_cancelamento_partidas",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_partidas_usuarios_excluida_definitivamente_por_usuario_id",
                table: "partidas");

            migrationBuilder.DropForeignKey(
                name: "FK_pendencias_usuarios_solicitacoes_cancelamento_partidas_soli~",
                table: "pendencias_usuarios");

            migrationBuilder.DropTable(
                name: "solicitacoes_cancelamento_partidas");

            migrationBuilder.DropIndex(
                name: "ix_pendencias_usuarios_cancelamento_atleta",
                table: "pendencias_usuarios");

            migrationBuilder.DropIndex(
                name: "IX_pendencias_usuarios_solicitacao_cancelamento_partida_id",
                table: "pendencias_usuarios");

            migrationBuilder.DropIndex(
                name: "IX_partidas_cancelada",
                table: "partidas");

            migrationBuilder.DropIndex(
                name: "IX_partidas_excluida_definitivamente_por_usuario_id",
                table: "partidas");

            migrationBuilder.DropIndex(
                name: "IX_partidas_solicitacao_cancelamento_origem_id",
                table: "partidas");

            migrationBuilder.DropColumn(
                name: "solicitacao_cancelamento_partida_id",
                table: "pendencias_usuarios");

            migrationBuilder.DropColumn(
                name: "cancelada",
                table: "partidas");

            migrationBuilder.DropColumn(
                name: "cancelada_em",
                table: "partidas");

            migrationBuilder.DropColumn(
                name: "excluida_definitivamente_em",
                table: "partidas");

            migrationBuilder.DropColumn(
                name: "excluida_definitivamente_por_usuario_id",
                table: "partidas");

            migrationBuilder.DropColumn(
                name: "motivo_exclusao_definitiva",
                table: "partidas");

            migrationBuilder.DropColumn(
                name: "solicitacao_cancelamento_origem_id",
                table: "partidas");
        }
    }
}
