using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class AdicionarInscricoesCampeonato : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "inscricoes_abertas",
                table: "competicoes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(@"
                UPDATE competicoes
                SET inscricoes_abertas = CASE
                    WHEN tipo = 1 THEN TRUE
                    ELSE FALSE
                END;
            ");

            migrationBuilder.CreateTable(
                name: "inscricoes_campeonato",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    competicao_id = table.Column<Guid>(type: "uuid", nullable: false),
                    categoria_competicao_id = table.Column<Guid>(type: "uuid", nullable: false),
                    atleta1_id = table.Column<Guid>(type: "uuid", nullable: false),
                    atleta2_id = table.Column<Guid>(type: "uuid", nullable: false),
                    data_inscricao_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    observacao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inscricoes_campeonato", x => x.id);
                    table.CheckConstraint("ck_inscricoes_campeonato_atletas_diferentes", "\"atleta1_id\" <> \"atleta2_id\"");
                    table.ForeignKey(
                        name: "FK_inscricoes_campeonato_atletas_atleta1_id",
                        column: x => x.atleta1_id,
                        principalTable: "atletas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inscricoes_campeonato_atletas_atleta2_id",
                        column: x => x.atleta2_id,
                        principalTable: "atletas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inscricoes_campeonato_categorias_competicao_categoria_compe~",
                        column: x => x.categoria_competicao_id,
                        principalTable: "categorias_competicao",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_inscricoes_campeonato_competicoes_competicao_id",
                        column: x => x.competicao_id,
                        principalTable: "competicoes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inscricoes_campeonato_atleta1_id",
                table: "inscricoes_campeonato",
                column: "atleta1_id");

            migrationBuilder.CreateIndex(
                name: "IX_inscricoes_campeonato_atleta2_id",
                table: "inscricoes_campeonato",
                column: "atleta2_id");

            migrationBuilder.CreateIndex(
                name: "IX_inscricoes_campeonato_categoria_competicao_id",
                table: "inscricoes_campeonato",
                column: "categoria_competicao_id");

            migrationBuilder.CreateIndex(
                name: "IX_inscricoes_campeonato_categoria_competicao_id_atleta1_id_at~",
                table: "inscricoes_campeonato",
                columns: new[] { "categoria_competicao_id", "atleta1_id", "atleta2_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inscricoes_campeonato_competicao_id",
                table: "inscricoes_campeonato",
                column: "competicao_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inscricoes_campeonato");

            migrationBuilder.DropColumn(
                name: "inscricoes_abertas",
                table: "competicoes");
        }
    }
}
