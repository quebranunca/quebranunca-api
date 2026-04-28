using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    [DbContext(typeof(PlataformaFutevoleiDbContext))]
    [Migration("20260323162000_AdicionarFormatosCampeonato")]
    public partial class AdicionarFormatosCampeonato : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "formatos_campeonato",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descricao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    tipo_formato = table.Column<int>(type: "integer", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    quantidade_grupos = table.Column<int>(type: "integer", nullable: true),
                    classificados_por_grupo = table.Column<int>(type: "integer", nullable: true),
                    gera_mata_mata_apos_grupos = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    turno_e_volta = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    tipo_chave = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    quantidade_derrotas_para_eliminacao = table.Column<int>(type: "integer", nullable: true),
                    permite_cabeca_de_chave = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    disputa_terceiro_lugar = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_formatos_campeonato", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_formatos_campeonato_nome",
                table: "formatos_campeonato",
                column: "nome",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "formatos_campeonato");
        }
    }
}
