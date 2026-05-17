using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class CorrigirMapeamentoExclusaoUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_grupos_atletas_competicoes_competicao_id",
                table: "grupos_atletas");

            migrationBuilder.DropIndex(
                name: "IX_solicitacoes_acesso_email",
                table: "solicitacoes_acesso");

            migrationBuilder.DropIndex(
                name: "IX_convites_cadastro_email",
                table: "convites_cadastro");

            migrationBuilder.DropColumn(
                name: "dados_anonimizados",
                table: "convites_cadastro");

            migrationBuilder.DropColumn(
                name: "excluido_em_utc",
                table: "convites_cadastro");

            migrationBuilder.DropColumn(
                name: "excluido_por_usuario_id",
                table: "convites_cadastro");

            migrationBuilder.RenameColumn(
                name: "competicao_id",
                table: "grupos_atletas",
                newName: "grupo_id");

            migrationBuilder.RenameIndex(
                name: "IX_grupos_atletas_competicao_id_atleta_id",
                table: "grupos_atletas",
                newName: "IX_grupos_atletas_grupo_id_atleta_id");

            migrationBuilder.RenameIndex(
                name: "IX_grupos_atletas_competicao_id",
                table: "grupos_atletas",
                newName: "IX_grupos_atletas_grupo_id");

            migrationBuilder.AlterColumn<Guid>(
                name: "categoria_competicao_id",
                table: "partidas",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "grupo_id",
                table: "partidas",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "grupos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    descricao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    link = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    data_inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_fim = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    local_id = table.Column<Guid>(type: "uuid", nullable: true),
                    usuario_organizador_id = table.Column<Guid>(type: "uuid", nullable: true),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grupos", x => x.id);
                    table.ForeignKey(
                        name: "FK_grupos_locais_local_id",
                        column: x => x.local_id,
                        principalTable: "locais",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_grupos_usuarios_usuario_organizador_id",
                        column: x => x.usuario_organizador_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_solicitacoes_acesso_email",
                table: "solicitacoes_acesso",
                column: "email",
                unique: true,
                filter: "status = 1");

            migrationBuilder.CreateIndex(
                name: "IX_partidas_grupo_id",
                table: "partidas",
                column: "grupo_id");

            migrationBuilder.CreateIndex(
                name: "IX_partidas_grupo_id_data_partida",
                table: "partidas",
                columns: new[] { "grupo_id", "data_partida" });

            migrationBuilder.CreateIndex(
                name: "IX_convites_cadastro_email",
                table: "convites_cadastro",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "IX_grupos_local_id",
                table: "grupos",
                column: "local_id");

            migrationBuilder.CreateIndex(
                name: "IX_grupos_usuario_organizador_id",
                table: "grupos",
                column: "usuario_organizador_id");

            migrationBuilder.AddForeignKey(
                name: "FK_grupos_atletas_grupos_grupo_id",
                table: "grupos_atletas",
                column: "grupo_id",
                principalTable: "grupos",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_partidas_grupos_grupo_id",
                table: "partidas",
                column: "grupo_id",
                principalTable: "grupos",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_grupos_atletas_grupos_grupo_id",
                table: "grupos_atletas");

            migrationBuilder.DropForeignKey(
                name: "FK_partidas_grupos_grupo_id",
                table: "partidas");

            migrationBuilder.DropTable(
                name: "grupos");

            migrationBuilder.DropIndex(
                name: "IX_solicitacoes_acesso_email",
                table: "solicitacoes_acesso");

            migrationBuilder.DropIndex(
                name: "IX_partidas_grupo_id",
                table: "partidas");

            migrationBuilder.DropIndex(
                name: "IX_partidas_grupo_id_data_partida",
                table: "partidas");

            migrationBuilder.DropIndex(
                name: "IX_convites_cadastro_email",
                table: "convites_cadastro");

            migrationBuilder.DropColumn(
                name: "grupo_id",
                table: "partidas");

            migrationBuilder.RenameColumn(
                name: "grupo_id",
                table: "grupos_atletas",
                newName: "competicao_id");

            migrationBuilder.RenameIndex(
                name: "IX_grupos_atletas_grupo_id_atleta_id",
                table: "grupos_atletas",
                newName: "IX_grupos_atletas_competicao_id_atleta_id");

            migrationBuilder.RenameIndex(
                name: "IX_grupos_atletas_grupo_id",
                table: "grupos_atletas",
                newName: "IX_grupos_atletas_competicao_id");

            migrationBuilder.AlterColumn<Guid>(
                name: "categoria_competicao_id",
                table: "partidas",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "dados_anonimizados",
                table: "convites_cadastro",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "excluido_em_utc",
                table: "convites_cadastro",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "excluido_por_usuario_id",
                table: "convites_cadastro",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_solicitacoes_acesso_email",
                table: "solicitacoes_acesso",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "IX_convites_cadastro_email",
                table: "convites_cadastro",
                column: "email",
                unique: true,
                filter: "status = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_grupos_atletas_competicoes_competicao_id",
                table: "grupos_atletas",
                column: "competicao_id",
                principalTable: "competicoes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
