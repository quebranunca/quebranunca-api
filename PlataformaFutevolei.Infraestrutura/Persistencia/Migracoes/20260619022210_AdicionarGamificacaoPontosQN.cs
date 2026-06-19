using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class AdicionarGamificacaoPontosQN : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "beneficios_pontuacao",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    titulo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    tipo = table.Column<int>(type: "integer", nullable: false),
                    pontos_necessarios = table.Column<int>(type: "integer", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    quantidade_disponivel = table.Column<int>(type: "integer", nullable: true),
                    imagem_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ordem = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    destaque = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_beneficios_pontuacao", x => x.id);
                    table.CheckConstraint("ck_beneficios_pontuacao_pontos_positivos", "\"pontos_necessarios\" > 0");
                    table.CheckConstraint("ck_beneficios_pontuacao_quantidade_nao_negativa", "\"quantidade_disponivel\" IS NULL OR \"quantidade_disponivel\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "pontuacoes_beneficios_atletas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    atleta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    saldo_atual = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_acumulado = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_resgatado = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pontuacoes_beneficios_atletas", x => x.id);
                    table.CheckConstraint("ck_pontuacoes_beneficios_atletas_saldo_nao_negativo", "\"saldo_atual\" >= 0");
                    table.CheckConstraint("ck_pontuacoes_beneficios_atletas_totais_nao_negativos", "\"total_acumulado\" >= 0 AND \"total_resgatado\" >= 0");
                    table.ForeignKey(
                        name: "FK_pontuacoes_beneficios_atletas_atletas_atleta_id",
                        column: x => x.atleta_id,
                        principalTable: "atletas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "resgates_beneficios_pontuacao",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    atleta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    beneficio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pontos_utilizados = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    codigo_cupom = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    observacao_atleta = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    observacao_admin = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    solicitado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    aprovado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejeitado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    utilizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    atualizado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resgates_beneficios_pontuacao", x => x.id);
                    table.CheckConstraint("ck_resgates_beneficios_pontuacao_pontos_positivos", "\"pontos_utilizados\" > 0");
                    table.ForeignKey(
                        name: "FK_resgates_beneficios_pontuacao_atletas_atleta_id",
                        column: x => x.atleta_id,
                        principalTable: "atletas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_resgates_beneficios_pontuacao_beneficios_pontuacao_benefici~",
                        column: x => x.beneficio_id,
                        principalTable: "beneficios_pontuacao",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_resgates_beneficios_pontuacao_usuarios_atualizado_por_usuar~",
                        column: x => x.atualizado_por_usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "extratos_pontuacao_beneficio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    atleta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    grupo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    partida_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resgate_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tipo_evento = table.Column<int>(type: "integer", nullable: false),
                    pontos = table.Column<int>(type: "integer", nullable: false),
                    descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    origem = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    chave_idempotencia = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    criado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_extratos_pontuacao_beneficio", x => x.id);
                    table.CheckConstraint("ck_extratos_pontuacao_beneficio_pontos_nao_zero", "\"pontos\" <> 0");
                    table.ForeignKey(
                        name: "FK_extratos_pontuacao_beneficio_atletas_atleta_id",
                        column: x => x.atleta_id,
                        principalTable: "atletas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_extratos_pontuacao_beneficio_grupos_grupo_id",
                        column: x => x.grupo_id,
                        principalTable: "grupos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_extratos_pontuacao_beneficio_partidas_partida_id",
                        column: x => x.partida_id,
                        principalTable: "partidas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_extratos_pontuacao_beneficio_resgates_beneficios_pontuacao_~",
                        column: x => x.resgate_id,
                        principalTable: "resgates_beneficios_pontuacao",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_extratos_pontuacao_beneficio_usuarios_criado_por_usuario_id",
                        column: x => x.criado_por_usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "beneficios_pontuacao",
                columns: new[] { "id", "ativo", "data_atualizacao", "data_criacao", "descricao", "destaque", "imagem_url", "ordem", "pontos_necessarios", "quantidade_disponivel", "tipo", "titulo" },
                values: new object[] { new Guid("11111111-1111-4111-8111-111111111111"), true, new DateTime(2026, 6, 19, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Solicite um cupom manual de desconto para usar na loja QuebraNunca.", true, null, 1, 600, null, 1, "10% OFF na loja" });

            migrationBuilder.InsertData(
                table: "beneficios_pontuacao",
                columns: new[] { "id", "ativo", "data_atualizacao", "data_criacao", "descricao", "imagem_url", "ordem", "pontos_necessarios", "quantidade_disponivel", "tipo", "titulo" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-4222-8222-222222222222"), true, new DateTime(2026, 6, 19, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Brinde sujeito a disponibilidade e aprovação manual.", null, 2, 1200, null, 2, "Boné QuebraNunca" },
                    { new Guid("33333333-3333-4333-8333-333333333333"), true, new DateTime(2026, 6, 19, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Brinde sujeito a disponibilidade e aprovação manual.", null, 3, 1200, null, 2, "Garrafa QN" },
                    { new Guid("44444444-4444-4444-8444-444444444444"), true, new DateTime(2026, 6, 19, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Produto sujeito a estoque e aprovação manual.", null, 4, 1800, null, 4, "Camiseta Drop Especial" },
                    { new Guid("55555555-5555-4555-8555-555555555555"), true, new DateTime(2026, 6, 19, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Experiência agendada manualmente pela equipe QuebraNunca.", null, 5, 2000, null, 3, "Aula com parceiro" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_beneficios_pontuacao_ativo",
                table: "beneficios_pontuacao",
                column: "ativo");

            migrationBuilder.CreateIndex(
                name: "IX_beneficios_pontuacao_ordem",
                table: "beneficios_pontuacao",
                column: "ordem");

            migrationBuilder.CreateIndex(
                name: "IX_beneficios_pontuacao_tipo",
                table: "beneficios_pontuacao",
                column: "tipo");

            migrationBuilder.CreateIndex(
                name: "IX_extratos_pontuacao_beneficio_atleta_id",
                table: "extratos_pontuacao_beneficio",
                column: "atleta_id");

            migrationBuilder.CreateIndex(
                name: "IX_extratos_pontuacao_beneficio_atleta_id_tipo_evento_data_cri~",
                table: "extratos_pontuacao_beneficio",
                columns: new[] { "atleta_id", "tipo_evento", "data_criacao" });

            migrationBuilder.CreateIndex(
                name: "IX_extratos_pontuacao_beneficio_chave_idempotencia",
                table: "extratos_pontuacao_beneficio",
                column: "chave_idempotencia",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_extratos_pontuacao_beneficio_criado_por_usuario_id",
                table: "extratos_pontuacao_beneficio",
                column: "criado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_extratos_pontuacao_beneficio_data_criacao",
                table: "extratos_pontuacao_beneficio",
                column: "data_criacao");

            migrationBuilder.CreateIndex(
                name: "IX_extratos_pontuacao_beneficio_grupo_id",
                table: "extratos_pontuacao_beneficio",
                column: "grupo_id");

            migrationBuilder.CreateIndex(
                name: "IX_extratos_pontuacao_beneficio_partida_id",
                table: "extratos_pontuacao_beneficio",
                column: "partida_id");

            migrationBuilder.CreateIndex(
                name: "IX_extratos_pontuacao_beneficio_resgate_id",
                table: "extratos_pontuacao_beneficio",
                column: "resgate_id");

            migrationBuilder.CreateIndex(
                name: "IX_extratos_pontuacao_beneficio_tipo_evento",
                table: "extratos_pontuacao_beneficio",
                column: "tipo_evento");

            migrationBuilder.CreateIndex(
                name: "IX_pontuacoes_beneficios_atletas_atleta_id",
                table: "pontuacoes_beneficios_atletas",
                column: "atleta_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_resgates_beneficios_pontuacao_atleta_id",
                table: "resgates_beneficios_pontuacao",
                column: "atleta_id");

            migrationBuilder.CreateIndex(
                name: "IX_resgates_beneficios_pontuacao_atleta_id_beneficio_id_status",
                table: "resgates_beneficios_pontuacao",
                columns: new[] { "atleta_id", "beneficio_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_resgates_beneficios_pontuacao_atualizado_por_usuario_id",
                table: "resgates_beneficios_pontuacao",
                column: "atualizado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_resgates_beneficios_pontuacao_beneficio_id",
                table: "resgates_beneficios_pontuacao",
                column: "beneficio_id");

            migrationBuilder.CreateIndex(
                name: "IX_resgates_beneficios_pontuacao_solicitado_em",
                table: "resgates_beneficios_pontuacao",
                column: "solicitado_em");

            migrationBuilder.CreateIndex(
                name: "IX_resgates_beneficios_pontuacao_status",
                table: "resgates_beneficios_pontuacao",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "extratos_pontuacao_beneficio");

            migrationBuilder.DropTable(
                name: "pontuacoes_beneficios_atletas");

            migrationBuilder.DropTable(
                name: "resgates_beneficios_pontuacao");

            migrationBuilder.DropTable(
                name: "beneficios_pontuacao");
        }
    }
}
