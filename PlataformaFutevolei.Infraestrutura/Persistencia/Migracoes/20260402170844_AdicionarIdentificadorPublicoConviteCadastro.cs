using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes;

public partial class AdicionarIdentificadorPublicoConviteCadastro : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "identificador_publico",
            table: "convites_cadastro",
            type: "character varying(40)",
            maxLength: 40,
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE convites_cadastro
            SET identificador_publico = REPLACE(id::text, '-', '')
            WHERE identificador_publico IS NULL;
            """);

        migrationBuilder.AlterColumn<string>(
            name: "identificador_publico",
            table: "convites_cadastro",
            type: "character varying(40)",
            maxLength: 40,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(40)",
            oldMaxLength: 40,
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_convites_cadastro_identificador_publico",
            table: "convites_cadastro",
            column: "identificador_publico",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_convites_cadastro_identificador_publico",
            table: "convites_cadastro");

        migrationBuilder.DropColumn(
            name: "identificador_publico",
            table: "convites_cadastro");
    }
}
