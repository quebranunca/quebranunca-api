using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes;

[DbContext(typeof(PlataformaFutevoleiDbContext))]
[Migration("20260402190000_ArmazenarCodigoConviteEmHash")]
public partial class ArmazenarCodigoConviteEmHash : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_convites_cadastro_token",
            table: "convites_cadastro");

        migrationBuilder.RenameColumn(
            name: "token",
            table: "convites_cadastro",
            newName: "codigo_convite_hash");

        migrationBuilder.AlterColumn<string>(
            name: "codigo_convite_hash",
            table: "convites_cadastro",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(80)",
            oldMaxLength: 80);

        migrationBuilder.Sql("""
            UPDATE convites_cadastro
            SET codigo_convite_hash = md5(lower(regexp_replace(codigo_convite_hash, '[^a-zA-Z0-9]', '', 'g')))
            WHERE codigo_convite_hash IS NOT NULL
              AND codigo_convite_hash <> '';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "codigo_convite_hash",
            table: "convites_cadastro",
            newName: "token");

        migrationBuilder.Sql("""
            UPDATE convites_cadastro
            SET token = md5(random()::text || clock_timestamp()::text || id::text)
            WHERE token IS NULL;
            """);

        migrationBuilder.AlterColumn<string>(
            name: "token",
            table: "convites_cadastro",
            type: "character varying(80)",
            maxLength: 80,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(64)",
            oldMaxLength: 64,
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_convites_cadastro_token",
            table: "convites_cadastro",
            column: "token",
            unique: true);
    }
}
