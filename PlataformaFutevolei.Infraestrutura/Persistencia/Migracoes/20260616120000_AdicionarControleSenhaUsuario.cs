using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes;

[DbContext(typeof(PlataformaFutevoleiDbContext))]
[Migration("20260616120000_AdicionarControleSenhaUsuario")]
public partial class AdicionarControleSenhaUsuario : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "senha_definida_em_utc",
            table: "usuarios",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "senha_atualizada_em_utc",
            table: "usuarios",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE usuarios
            SET senha_definida_em_utc = COALESCE(data_atualizacao, data_criacao),
                senha_atualizada_em_utc = COALESCE(data_atualizacao, data_criacao)
            WHERE senha_hash IS NOT NULL
              AND btrim(senha_hash) <> ''
              AND perfil = 1;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "senha_atualizada_em_utc",
            table: "usuarios");

        migrationBuilder.DropColumn(
            name: "senha_definida_em_utc",
            table: "usuarios");
    }
}
