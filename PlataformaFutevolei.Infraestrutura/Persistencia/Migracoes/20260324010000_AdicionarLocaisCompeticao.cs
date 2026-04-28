using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    [DbContext(typeof(PlataformaFutevoleiDbContext))]
    [Migration("20260324010000_AdicionarLocaisCompeticao")]
    public partial class AdicionarLocaisCompeticao : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "locais",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tipo = table.Column<int>(type: "integer", nullable: false),
                    quantidade_quadras = table.Column<int>(type: "integer", nullable: false),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_locais", x => x.id);
                });

            migrationBuilder.AddColumn<Guid>(
                name: "local_id",
                table: "competicoes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_competicoes_local_id",
                table: "competicoes",
                column: "local_id");

            migrationBuilder.CreateIndex(
                name: "IX_locais_nome",
                table: "locais",
                column: "nome",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_competicoes_locais_local_id",
                table: "competicoes",
                column: "local_id",
                principalTable: "locais",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_competicoes_locais_local_id",
                table: "competicoes");

            migrationBuilder.DropTable(
                name: "locais");

            migrationBuilder.DropIndex(
                name: "IX_competicoes_local_id",
                table: "competicoes");

            migrationBuilder.DropColumn(
                name: "local_id",
                table: "competicoes");
        }
    }
}
