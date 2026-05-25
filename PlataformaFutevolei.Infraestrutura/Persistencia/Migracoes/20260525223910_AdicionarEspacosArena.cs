using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class AdicionarEspacosArena : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "arena_espacos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    arena_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tipo_espaco = table.Column<int>(type: "integer", nullable: false),
                    descricao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    possui_iluminacao = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    possui_cobertura = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ordem_exibicao = table.Column<int>(type: "integer", nullable: true),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_arena_espacos", x => x.id);
                    table.ForeignKey(
                        name: "FK_arena_espacos_arenas_arena_id",
                        column: x => x.arena_id,
                        principalTable: "arenas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_arena_espacos_arena_id_ativo",
                table: "arena_espacos",
                columns: new[] { "arena_id", "ativo" });

            migrationBuilder.CreateIndex(
                name: "IX_arena_espacos_arena_id_ordem_exibicao",
                table: "arena_espacos",
                columns: new[] { "arena_id", "ordem_exibicao" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "arena_espacos");
        }
    }
}
