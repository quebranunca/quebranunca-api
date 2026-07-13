using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class AdicionarHistoricoPartidas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "historicos_partidas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    partida_id_original = table.Column<Guid>(type: "uuid", nullable: false),
                    acao = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    usuario_responsavel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    data_hora_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    snapshot_json = table.Column<string>(type: "jsonb", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historicos_partidas", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_historicos_partidas_acao",
                table: "historicos_partidas",
                column: "acao");

            migrationBuilder.CreateIndex(
                name: "IX_historicos_partidas_data_hora_utc",
                table: "historicos_partidas",
                column: "data_hora_utc");

            migrationBuilder.CreateIndex(
                name: "IX_historicos_partidas_partida_id_original",
                table: "historicos_partidas",
                column: "partida_id_original");

            migrationBuilder.CreateIndex(
                name: "IX_historicos_partidas_usuario_responsavel_id",
                table: "historicos_partidas",
                column: "usuario_responsavel_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "historicos_partidas");
        }
    }
}
