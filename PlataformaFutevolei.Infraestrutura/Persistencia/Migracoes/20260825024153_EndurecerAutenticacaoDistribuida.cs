using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class EndurecerAutenticacaoDistribuida : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "versao_seguranca",
                table: "usuarios",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "controles_rate_limit",
                columns: table => new
                {
                    chave = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    janela_inicio_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    contador = table.Column<int>(type: "integer", nullable: false),
                    expira_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_controles_rate_limit", x => x.chave);
                });

            migrationBuilder.CreateIndex(
                name: "IX_controles_rate_limit_expira_em_utc",
                table: "controles_rate_limit",
                column: "expira_em_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "controles_rate_limit");

            migrationBuilder.DropColumn(
                name: "versao_seguranca",
                table: "usuarios");
        }
    }
}
