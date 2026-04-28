using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "atletas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    apelido = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    cidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_atletas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "competicoes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tipo = table.Column<int>(type: "integer", nullable: false),
                    descricao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    data_inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_fim = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competicoes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    senha_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    perfil = table.Column<int>(type: "integer", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "duplas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    atleta1_id = table.Column<Guid>(type: "uuid", nullable: false),
                    atleta2_id = table.Column<Guid>(type: "uuid", nullable: false),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_duplas", x => x.id);
                    table.CheckConstraint("ck_duplas_atletas_diferentes", "\"atleta1_id\" <> \"atleta2_id\"");
                    table.ForeignKey(
                        name: "FK_duplas_atletas_atleta1_id",
                        column: x => x.atleta1_id,
                        principalTable: "atletas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_duplas_atletas_atleta2_id",
                        column: x => x.atleta2_id,
                        principalTable: "atletas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "categorias_competicao",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    competicao_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    genero = table.Column<int>(type: "integer", nullable: false),
                    nivel = table.Column<int>(type: "integer", nullable: false),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorias_competicao", x => x.id);
                    table.ForeignKey(
                        name: "FK_categorias_competicao_competicoes_competicao_id",
                        column: x => x.competicao_id,
                        principalTable: "competicoes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "partidas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    categoria_competicao_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dupla_a_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dupla_b_id = table.Column<Guid>(type: "uuid", nullable: false),
                    placar_dupla_a = table.Column<int>(type: "integer", nullable: false),
                    placar_dupla_b = table.Column<int>(type: "integer", nullable: false),
                    dupla_vencedora_id = table.Column<Guid>(type: "uuid", nullable: false),
                    data_partida = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    observacoes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partidas", x => x.id);
                    table.CheckConstraint("ck_partidas_duplas_diferentes", "\"dupla_a_id\" <> \"dupla_b_id\"");
                    table.CheckConstraint("ck_partidas_vencedora_valida", "\"dupla_vencedora_id\" = \"dupla_a_id\" OR \"dupla_vencedora_id\" = \"dupla_b_id\"");
                    table.ForeignKey(
                        name: "FK_partidas_categorias_competicao_categoria_competicao_id",
                        column: x => x.categoria_competicao_id,
                        principalTable: "categorias_competicao",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_partidas_duplas_dupla_a_id",
                        column: x => x.dupla_a_id,
                        principalTable: "duplas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_partidas_duplas_dupla_b_id",
                        column: x => x.dupla_b_id,
                        principalTable: "duplas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_partidas_duplas_dupla_vencedora_id",
                        column: x => x.dupla_vencedora_id,
                        principalTable: "duplas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_categorias_competicao_competicao_id",
                table: "categorias_competicao",
                column: "competicao_id");

            migrationBuilder.CreateIndex(
                name: "IX_duplas_atleta1_id",
                table: "duplas",
                column: "atleta1_id");

            migrationBuilder.CreateIndex(
                name: "IX_duplas_atleta1_id_atleta2_id",
                table: "duplas",
                columns: new[] { "atleta1_id", "atleta2_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_duplas_atleta2_id",
                table: "duplas",
                column: "atleta2_id");

            migrationBuilder.CreateIndex(
                name: "IX_partidas_categoria_competicao_id",
                table: "partidas",
                column: "categoria_competicao_id");

            migrationBuilder.CreateIndex(
                name: "IX_partidas_dupla_a_id",
                table: "partidas",
                column: "dupla_a_id");

            migrationBuilder.CreateIndex(
                name: "IX_partidas_dupla_b_id",
                table: "partidas",
                column: "dupla_b_id");

            migrationBuilder.CreateIndex(
                name: "IX_partidas_dupla_vencedora_id",
                table: "partidas",
                column: "dupla_vencedora_id");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_email",
                table: "usuarios",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "partidas");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "categorias_competicao");

            migrationBuilder.DropTable(
                name: "duplas");

            migrationBuilder.DropTable(
                name: "competicoes");

            migrationBuilder.DropTable(
                name: "atletas");
        }
    }
}
