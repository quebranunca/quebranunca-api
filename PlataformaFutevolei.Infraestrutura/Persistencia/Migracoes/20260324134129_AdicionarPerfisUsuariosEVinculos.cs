using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes;

public partial class AdicionarPerfisUsuariosEVinculos : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "atleta_id",
            table: "usuarios",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "usuario_organizador_id",
            table: "competicoes",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "cpf",
            table: "atletas",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "email",
            table: "atletas",
            type: "character varying(150)",
            maxLength: 150,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "instagram",
            table: "atletas",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "telefone",
            table: "atletas",
            type: "character varying(30)",
            maxLength: 30,
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "dupla_id",
            table: "inscricoes_campeonato",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "pago",
            table: "inscricoes_campeonato",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.Sql("""
            UPDATE usuarios
            SET perfil = 3
            WHERE perfil = 2;
            """);

        migrationBuilder.Sql("""
            INSERT INTO duplas (id, nome, atleta1_id, atleta2_id, data_criacao, data_atualizacao)
            SELECT
                CAST(
                    SUBSTRING(MD5(CONCAT(pares.atleta1_id::text, ':', pares.atleta2_id::text)), 1, 8) || '-' ||
                    SUBSTRING(MD5(CONCAT(pares.atleta1_id::text, ':', pares.atleta2_id::text)), 9, 4) || '-' ||
                    SUBSTRING(MD5(CONCAT(pares.atleta1_id::text, ':', pares.atleta2_id::text)), 13, 4) || '-' ||
                    SUBSTRING(MD5(CONCAT(pares.atleta1_id::text, ':', pares.atleta2_id::text)), 17, 4) || '-' ||
                    SUBSTRING(MD5(CONCAT(pares.atleta1_id::text, ':', pares.atleta2_id::text)), 21, 12)
                    AS uuid
                ),
                CONCAT(atleta_a.nome, ' / ', atleta_b.nome),
                pares.atleta1_id,
                pares.atleta2_id,
                NOW(),
                NOW()
            FROM (
                SELECT DISTINCT
                    LEAST(inscricao.atleta1_id, inscricao.atleta2_id) AS atleta1_id,
                    GREATEST(inscricao.atleta1_id, inscricao.atleta2_id) AS atleta2_id
                FROM inscricoes_campeonato inscricao
            ) pares
            INNER JOIN atletas atleta_a ON atleta_a.id = pares.atleta1_id
            INNER JOIN atletas atleta_b ON atleta_b.id = pares.atleta2_id
            LEFT JOIN duplas dupla_existente
                ON dupla_existente.atleta1_id = pares.atleta1_id
               AND dupla_existente.atleta2_id = pares.atleta2_id
            WHERE dupla_existente.id IS NULL;
            """);

        migrationBuilder.Sql("""
            UPDATE inscricoes_campeonato inscricao
            SET dupla_id = dupla.id
            FROM duplas dupla
            WHERE dupla.atleta1_id = LEAST(inscricao.atleta1_id, inscricao.atleta2_id)
              AND dupla.atleta2_id = GREATEST(inscricao.atleta1_id, inscricao.atleta2_id);
            """);

        migrationBuilder.Sql("""
            DELETE FROM inscricoes_campeonato inscricao
            USING (
                SELECT id
                FROM (
                    SELECT
                        id,
                        ROW_NUMBER() OVER (
                            PARTITION BY categoria_competicao_id, dupla_id
                            ORDER BY data_inscricao_utc, data_criacao, id
                        ) AS ordem
                    FROM inscricoes_campeonato
                    WHERE dupla_id IS NOT NULL
                ) duplicadas
                WHERE duplicadas.ordem > 1
            ) itens_remover
            WHERE inscricao.id = itens_remover.id;
            """);

        migrationBuilder.DropForeignKey(
            name: "FK_inscricoes_campeonato_atletas_atleta1_id",
            table: "inscricoes_campeonato");

        migrationBuilder.DropForeignKey(
            name: "FK_inscricoes_campeonato_atletas_atleta2_id",
            table: "inscricoes_campeonato");

        migrationBuilder.DropIndex(
            name: "IX_inscricoes_campeonato_atleta1_id",
            table: "inscricoes_campeonato");

        migrationBuilder.DropIndex(
            name: "IX_inscricoes_campeonato_atleta2_id",
            table: "inscricoes_campeonato");

        migrationBuilder.DropIndex(
            name: "IX_inscricoes_campeonato_categoria_competicao_id_atleta1_id_at~",
            table: "inscricoes_campeonato");

        migrationBuilder.DropCheckConstraint(
            name: "ck_inscricoes_campeonato_atletas_diferentes",
            table: "inscricoes_campeonato");

        migrationBuilder.AlterColumn<Guid>(
            name: "dupla_id",
            table: "inscricoes_campeonato",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.DropColumn(
            name: "atleta1_id",
            table: "inscricoes_campeonato");

        migrationBuilder.DropColumn(
            name: "atleta2_id",
            table: "inscricoes_campeonato");

        migrationBuilder.CreateIndex(
            name: "IX_usuarios_atleta_id",
            table: "usuarios",
            column: "atleta_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_competicoes_usuario_organizador_id",
            table: "competicoes",
            column: "usuario_organizador_id");

        migrationBuilder.CreateIndex(
            name: "IX_atletas_cpf",
            table: "atletas",
            column: "cpf");

        migrationBuilder.CreateIndex(
            name: "IX_inscricoes_campeonato_dupla_id",
            table: "inscricoes_campeonato",
            column: "dupla_id");

        migrationBuilder.CreateIndex(
            name: "IX_inscricoes_campeonato_categoria_competicao_id_dupla_id",
            table: "inscricoes_campeonato",
            columns: new[] { "categoria_competicao_id", "dupla_id" },
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_competicoes_usuarios_usuario_organizador_id",
            table: "competicoes",
            column: "usuario_organizador_id",
            principalTable: "usuarios",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "FK_usuarios_atletas_atleta_id",
            table: "usuarios",
            column: "atleta_id",
            principalTable: "atletas",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "FK_inscricoes_campeonato_duplas_dupla_id",
            table: "inscricoes_campeonato",
            column: "dupla_id",
            principalTable: "duplas",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_competicoes_usuarios_usuario_organizador_id",
            table: "competicoes");

        migrationBuilder.DropForeignKey(
            name: "FK_usuarios_atletas_atleta_id",
            table: "usuarios");

        migrationBuilder.DropForeignKey(
            name: "FK_inscricoes_campeonato_duplas_dupla_id",
            table: "inscricoes_campeonato");

        migrationBuilder.DropIndex(
            name: "IX_usuarios_atleta_id",
            table: "usuarios");

        migrationBuilder.DropIndex(
            name: "IX_competicoes_usuario_organizador_id",
            table: "competicoes");

        migrationBuilder.DropIndex(
            name: "IX_atletas_cpf",
            table: "atletas");

        migrationBuilder.DropIndex(
            name: "IX_inscricoes_campeonato_dupla_id",
            table: "inscricoes_campeonato");

        migrationBuilder.DropIndex(
            name: "IX_inscricoes_campeonato_categoria_competicao_id_dupla_id",
            table: "inscricoes_campeonato");

        migrationBuilder.AddColumn<Guid>(
            name: "atleta1_id",
            table: "inscricoes_campeonato",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "atleta2_id",
            table: "inscricoes_campeonato",
            type: "uuid",
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE inscricoes_campeonato inscricao
            SET atleta1_id = dupla.atleta1_id,
                atleta2_id = dupla.atleta2_id
            FROM duplas dupla
            WHERE dupla.id = inscricao.dupla_id;
            """);

        migrationBuilder.AlterColumn<Guid>(
            name: "atleta1_id",
            table: "inscricoes_campeonato",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "atleta2_id",
            table: "inscricoes_campeonato",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.DropColumn(
            name: "dupla_id",
            table: "inscricoes_campeonato");

        migrationBuilder.DropColumn(
            name: "pago",
            table: "inscricoes_campeonato");

        migrationBuilder.DropColumn(
            name: "atleta_id",
            table: "usuarios");

        migrationBuilder.DropColumn(
            name: "usuario_organizador_id",
            table: "competicoes");

        migrationBuilder.DropColumn(
            name: "cpf",
            table: "atletas");

        migrationBuilder.DropColumn(
            name: "email",
            table: "atletas");

        migrationBuilder.DropColumn(
            name: "instagram",
            table: "atletas");

        migrationBuilder.DropColumn(
            name: "telefone",
            table: "atletas");

        migrationBuilder.CreateIndex(
            name: "IX_inscricoes_campeonato_atleta1_id",
            table: "inscricoes_campeonato",
            column: "atleta1_id");

        migrationBuilder.CreateIndex(
            name: "IX_inscricoes_campeonato_atleta2_id",
            table: "inscricoes_campeonato",
            column: "atleta2_id");

        migrationBuilder.CreateIndex(
            name: "IX_inscricoes_campeonato_categoria_competicao_id_atleta1_id_at~",
            table: "inscricoes_campeonato",
            columns: new[] { "categoria_competicao_id", "atleta1_id", "atleta2_id" },
            unique: true);

        migrationBuilder.AddCheckConstraint(
            name: "ck_inscricoes_campeonato_atletas_diferentes",
            table: "inscricoes_campeonato",
            sql: "\"atleta1_id\" <> \"atleta2_id\"");

        migrationBuilder.AddForeignKey(
            name: "FK_inscricoes_campeonato_atletas_atleta1_id",
            table: "inscricoes_campeonato",
            column: "atleta1_id",
            principalTable: "atletas",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_inscricoes_campeonato_atletas_atleta2_id",
            table: "inscricoes_campeonato",
            column: "atleta2_id",
            principalTable: "atletas",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.Sql("""
            UPDATE usuarios
            SET perfil = 2
            WHERE perfil = 3;
            """);
    }
}
