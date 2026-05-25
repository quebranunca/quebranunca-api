using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes;

public partial class EvoluirLocalParaArena : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_competicoes_locais_local_id",
            table: "competicoes");

        migrationBuilder.DropForeignKey(
            name: "FK_grupos_locais_local_id",
            table: "grupos");

        migrationBuilder.RenameTable(
            name: "locais",
            newName: "arenas");

        migrationBuilder.DropPrimaryKey(name: "PK_locais", table: "arenas");
        migrationBuilder.AddPrimaryKey(name: "PK_arenas", table: "arenas", column: "id");

        migrationBuilder.RenameColumn(
            name: "local_id",
            table: "competicoes",
            newName: "arena_id");

        migrationBuilder.RenameIndex(
            name: "IX_competicoes_local_id",
            table: "competicoes",
            newName: "IX_competicoes_arena_id");

        migrationBuilder.RenameColumn(
            name: "local_id",
            table: "grupos",
            newName: "arena_id");

        migrationBuilder.RenameIndex(
            name: "IX_grupos_local_id",
            table: "grupos",
            newName: "IX_grupos_arena_id");

        migrationBuilder.RenameColumn(
            name: "tipo",
            table: "arenas",
            newName: "tipo_arena");

        migrationBuilder.RenameColumn(
            name: "quantidade_quadras",
            table: "arenas",
            newName: "quantidade_espacos");

        migrationBuilder.RenameIndex(
            name: "IX_locais_nome",
            table: "arenas",
            newName: "IX_arenas_nome");

        migrationBuilder.AddColumn<string>(
            name: "slug",
            table: "arenas",
            type: "character varying(220)",
            maxLength: 220,
            nullable: true);

        migrationBuilder.AddColumn<string>(name: "descricao", table: "arenas", type: "character varying(1000)", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<string>(name: "endereco", table: "arenas", type: "character varying(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<string>(name: "endereco_resumo", table: "arenas", type: "character varying(250)", maxLength: 250, nullable: true);
        migrationBuilder.AddColumn<string>(name: "cidade", table: "arenas", type: "character varying(100)", maxLength: 100, nullable: true);
        migrationBuilder.AddColumn<string>(name: "estado", table: "arenas", type: "character varying(50)", maxLength: 50, nullable: true);
        migrationBuilder.AddColumn<double>(name: "latitude", table: "arenas", type: "double precision", nullable: true);
        migrationBuilder.AddColumn<double>(name: "longitude", table: "arenas", type: "double precision", nullable: true);
        migrationBuilder.AddColumn<string>(name: "whatsapp", table: "arenas", type: "character varying(30)", maxLength: 30, nullable: true);
        migrationBuilder.AddColumn<string>(name: "instagram", table: "arenas", type: "character varying(100)", maxLength: 100, nullable: true);
        migrationBuilder.AddColumn<string>(name: "site", table: "arenas", type: "character varying(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<string>(name: "logo_url", table: "arenas", type: "character varying(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<string>(name: "logo_public_id", table: "arenas", type: "character varying(255)", maxLength: 255, nullable: true);
        migrationBuilder.AddColumn<string>(name: "capa_url", table: "arenas", type: "character varying(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<string>(name: "capa_public_id", table: "arenas", type: "character varying(255)", maxLength: 255, nullable: true);
        migrationBuilder.AddColumn<bool>(name: "publica", table: "arenas", type: "boolean", nullable: false, defaultValue: true);
        migrationBuilder.AddColumn<bool>(name: "ativa", table: "arenas", type: "boolean", nullable: false, defaultValue: true);

        migrationBuilder.Sql("""
            UPDATE arenas
            SET tipo_arena = CASE tipo_arena
                WHEN 1 THEN 3
                WHEN 2 THEN 2
                WHEN 3 THEN 5
                WHEN 4 THEN 8
                ELSE 9
            END,
            slug = 'arena-' || replace(id::text, '-', '');
            """);

        migrationBuilder.AlterColumn<string>(
            name: "slug",
            table: "arenas",
            type: "character varying(220)",
            maxLength: 220,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(220)",
            oldMaxLength: 220,
            oldNullable: true);

        migrationBuilder.CreateTable(
            name: "arena_responsaveis",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                arena_id = table.Column<Guid>(type: "uuid", nullable: false),
                usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                papel = table.Column<int>(type: "integer", nullable: false),
                ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_arena_responsaveis", x => x.id);
                table.ForeignKey(
                    name: "FK_arena_responsaveis_arenas_arena_id",
                    column: x => x.arena_id,
                    principalTable: "arenas",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_arena_responsaveis_usuarios_usuario_id",
                    column: x => x.usuario_id,
                    principalTable: "usuarios",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.Sql("""
            INSERT INTO arena_responsaveis (
                id, arena_id, usuario_id, papel, ativo, data_criacao, data_atualizacao)
            SELECT
                id, id, usuario_criador_id, 1, TRUE, data_criacao, data_atualizacao
            FROM arenas
            WHERE usuario_criador_id IS NOT NULL;
            """);

        migrationBuilder.CreateIndex(
            name: "IX_arena_responsaveis_arena_id_usuario_id_papel",
            table: "arena_responsaveis",
            columns: new[] { "arena_id", "usuario_id", "papel" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_arena_responsaveis_usuario_id",
            table: "arena_responsaveis",
            column: "usuario_id");

        migrationBuilder.CreateIndex(
            name: "IX_arenas_slug",
            table: "arenas",
            column: "slug",
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_competicoes_arenas_arena_id",
            table: "competicoes",
            column: "arena_id",
            principalTable: "arenas",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "FK_grupos_arenas_arena_id",
            table: "grupos",
            column: "arena_id",
            principalTable: "arenas",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_competicoes_arenas_arena_id", table: "competicoes");
        migrationBuilder.DropForeignKey(name: "FK_grupos_arenas_arena_id", table: "grupos");

        migrationBuilder.DropTable(name: "arena_responsaveis");
        migrationBuilder.DropIndex(name: "IX_arenas_slug", table: "arenas");

        migrationBuilder.DropColumn(name: "slug", table: "arenas");
        migrationBuilder.DropColumn(name: "descricao", table: "arenas");
        migrationBuilder.DropColumn(name: "endereco", table: "arenas");
        migrationBuilder.DropColumn(name: "endereco_resumo", table: "arenas");
        migrationBuilder.DropColumn(name: "cidade", table: "arenas");
        migrationBuilder.DropColumn(name: "estado", table: "arenas");
        migrationBuilder.DropColumn(name: "latitude", table: "arenas");
        migrationBuilder.DropColumn(name: "longitude", table: "arenas");
        migrationBuilder.DropColumn(name: "whatsapp", table: "arenas");
        migrationBuilder.DropColumn(name: "instagram", table: "arenas");
        migrationBuilder.DropColumn(name: "site", table: "arenas");
        migrationBuilder.DropColumn(name: "logo_url", table: "arenas");
        migrationBuilder.DropColumn(name: "logo_public_id", table: "arenas");
        migrationBuilder.DropColumn(name: "capa_url", table: "arenas");
        migrationBuilder.DropColumn(name: "capa_public_id", table: "arenas");
        migrationBuilder.DropColumn(name: "publica", table: "arenas");
        migrationBuilder.DropColumn(name: "ativa", table: "arenas");

        migrationBuilder.Sql("""
            UPDATE arenas
            SET tipo_arena = CASE tipo_arena
                WHEN 3 THEN 1
                WHEN 2 THEN 2
                WHEN 5 THEN 3
                WHEN 8 THEN 4
                ELSE 1
            END;
            """);

        migrationBuilder.RenameColumn(name: "tipo_arena", table: "arenas", newName: "tipo");
        migrationBuilder.RenameColumn(name: "quantidade_espacos", table: "arenas", newName: "quantidade_quadras");
        migrationBuilder.RenameIndex(name: "IX_arenas_nome", table: "arenas", newName: "IX_locais_nome");
        migrationBuilder.DropPrimaryKey(name: "PK_arenas", table: "arenas");
        migrationBuilder.RenameTable(name: "arenas", newName: "locais");
        migrationBuilder.AddPrimaryKey(name: "PK_locais", table: "locais", column: "id");

        migrationBuilder.RenameColumn(name: "arena_id", table: "competicoes", newName: "local_id");
        migrationBuilder.RenameIndex(name: "IX_competicoes_arena_id", table: "competicoes", newName: "IX_competicoes_local_id");
        migrationBuilder.RenameColumn(name: "arena_id", table: "grupos", newName: "local_id");
        migrationBuilder.RenameIndex(name: "IX_grupos_arena_id", table: "grupos", newName: "IX_grupos_local_id");

        migrationBuilder.AddForeignKey(
            name: "FK_competicoes_locais_local_id",
            table: "competicoes",
            column: "local_id",
            principalTable: "locais",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "FK_grupos_locais_local_id",
            table: "grupos",
            column: "local_id",
            principalTable: "locais",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);
    }
}
