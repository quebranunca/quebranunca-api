using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes;

[DbContext(typeof(PlataformaFutevoleiDbContext))]
[Migration("20260503120000_SepararGruposDeCompeticoes")]
public partial class SepararGruposDeCompeticoes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
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

        migrationBuilder.Sql("""
            INSERT INTO grupos (
                id, nome, descricao, link, data_inicio, data_fim, local_id,
                usuario_organizador_id, data_criacao, data_atualizacao)
            SELECT
                id, nome, descricao, link, data_inicio, data_fim, local_id,
                usuario_organizador_id, data_criacao, data_atualizacao
            FROM competicoes
            WHERE tipo = 3
              AND NOT EXISTS (
                  SELECT 1 FROM grupos WHERE grupos.id = competicoes.id
              );
            """);

        migrationBuilder.AddColumn<Guid>(
            name: "grupo_id",
            table: "partidas",
            type: "uuid",
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE partidas
            SET grupo_id = categorias_competicao.competicao_id
            FROM categorias_competicao
            INNER JOIN competicoes ON competicoes.id = categorias_competicao.competicao_id
            WHERE partidas.categoria_competicao_id = categorias_competicao.id
              AND competicoes.tipo = 3;
            """);

        migrationBuilder.DropForeignKey(
            name: "FK_partidas_categorias_competicao_categoria_competicao_id",
            table: "partidas");

        migrationBuilder.AlterColumn<Guid>(
            name: "categoria_competicao_id",
            table: "partidas",
            type: "uuid",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.Sql("""
            UPDATE partidas
            SET categoria_competicao_id = NULL
            WHERE grupo_id IS NOT NULL;
            """);

        migrationBuilder.AddForeignKey(
            name: "FK_partidas_categorias_competicao_categoria_competicao_id",
            table: "partidas",
            column: "categoria_competicao_id",
            principalTable: "categorias_competicao",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.DropForeignKey(
            name: "FK_grupos_atletas_competicoes_competicao_id",
            table: "grupos_atletas");

        migrationBuilder.DropIndex(
            name: "IX_grupos_atletas_competicao_id",
            table: "grupos_atletas");

        migrationBuilder.DropIndex(
            name: "IX_grupos_atletas_competicao_id_atleta_id",
            table: "grupos_atletas");

        migrationBuilder.AddColumn<Guid>(
            name: "grupo_id",
            table: "grupos_atletas",
            type: "uuid",
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE grupos_atletas
            SET grupo_id = competicao_id
            WHERE EXISTS (
                SELECT 1 FROM grupos WHERE grupos.id = grupos_atletas.competicao_id
            );
            """);

        migrationBuilder.AlterColumn<Guid>(
            name: "grupo_id",
            table: "grupos_atletas",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.DropColumn(
            name: "competicao_id",
            table: "grupos_atletas");

        migrationBuilder.CreateIndex(
            name: "IX_grupos_local_id",
            table: "grupos",
            column: "local_id");

        migrationBuilder.CreateIndex(
            name: "IX_grupos_usuario_organizador_id",
            table: "grupos",
            column: "usuario_organizador_id");

        migrationBuilder.CreateIndex(
            name: "IX_partidas_grupo_id",
            table: "partidas",
            column: "grupo_id");

        migrationBuilder.CreateIndex(
            name: "IX_partidas_grupo_id_data_partida",
            table: "partidas",
            columns: new[] { "grupo_id", "data_partida" });

        migrationBuilder.CreateIndex(
            name: "IX_grupos_atletas_grupo_id",
            table: "grupos_atletas",
            column: "grupo_id");

        migrationBuilder.CreateIndex(
            name: "IX_grupos_atletas_grupo_id_atleta_id",
            table: "grupos_atletas",
            columns: new[] { "grupo_id", "atleta_id" },
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_partidas_grupos_grupo_id",
            table: "partidas",
            column: "grupo_id",
            principalTable: "grupos",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_grupos_atletas_grupos_grupo_id",
            table: "grupos_atletas",
            column: "grupo_id",
            principalTable: "grupos",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_partidas_grupos_grupo_id", table: "partidas");
        migrationBuilder.DropForeignKey(name: "FK_grupos_atletas_grupos_grupo_id", table: "grupos_atletas");

        migrationBuilder.DropIndex(name: "IX_partidas_grupo_id", table: "partidas");
        migrationBuilder.DropIndex(name: "IX_partidas_grupo_id_data_partida", table: "partidas");
        migrationBuilder.DropIndex(name: "IX_grupos_atletas_grupo_id", table: "grupos_atletas");
        migrationBuilder.DropIndex(name: "IX_grupos_atletas_grupo_id_atleta_id", table: "grupos_atletas");

        migrationBuilder.AddColumn<Guid>(
            name: "competicao_id",
            table: "grupos_atletas",
            type: "uuid",
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE grupos_atletas
            SET competicao_id = grupo_id;

            UPDATE partidas
            SET categoria_competicao_id = categorias_competicao.id
            FROM categorias_competicao
            WHERE partidas.grupo_id = categorias_competicao.competicao_id
              AND categorias_competicao.nome = 'Sem categoria'
              AND partidas.categoria_competicao_id IS NULL;
            """);

        migrationBuilder.AlterColumn<Guid>(
            name: "competicao_id",
            table: "grupos_atletas",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.DropColumn(name: "grupo_id", table: "grupos_atletas");
        migrationBuilder.DropColumn(name: "grupo_id", table: "partidas");

        migrationBuilder.AlterColumn<Guid>(
            name: "categoria_competicao_id",
            table: "partidas",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.CreateIndex(name: "IX_grupos_atletas_competicao_id", table: "grupos_atletas", column: "competicao_id");
        migrationBuilder.CreateIndex(
            name: "IX_grupos_atletas_competicao_id_atleta_id",
            table: "grupos_atletas",
            columns: new[] { "competicao_id", "atleta_id" },
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_partidas_categorias_competicao_categoria_competicao_id",
            table: "partidas",
            column: "categoria_competicao_id",
            principalTable: "categorias_competicao",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_grupos_atletas_competicoes_competicao_id",
            table: "grupos_atletas",
            column: "competicao_id",
            principalTable: "competicoes",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.DropTable(name: "grupos");
    }
}
