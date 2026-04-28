using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes;

[DbContext(typeof(PlataformaFutevoleiDbContext))]
[Migration("20260326011500_AdicionarAprovacaoTabelaJogosCategoria")]
public partial class AdicionarAprovacaoTabelaJogosCategoria : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "tabela_jogos_aprovada_em_utc",
            table: "categorias_competicao",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "tabela_jogos_aprovada_por_usuario_id",
            table: "categorias_competicao",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_categorias_competicao_tabela_jogos_aprovada_por_usuario_id",
            table: "categorias_competicao",
            column: "tabela_jogos_aprovada_por_usuario_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_categorias_competicao_tabela_jogos_aprovada_por_usuario_id",
            table: "categorias_competicao");

        migrationBuilder.DropColumn(
            name: "tabela_jogos_aprovada_em_utc",
            table: "categorias_competicao");

        migrationBuilder.DropColumn(
            name: "tabela_jogos_aprovada_por_usuario_id",
            table: "categorias_competicao");
    }
}
