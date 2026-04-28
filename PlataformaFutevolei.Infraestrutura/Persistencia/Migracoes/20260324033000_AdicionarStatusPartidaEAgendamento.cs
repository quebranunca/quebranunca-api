using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    [DbContext(typeof(PlataformaFutevoleiDbContext))]
    [Migration("20260324033000_AdicionarStatusPartidaEAgendamento")]
    public partial class AdicionarStatusPartidaEAgendamento : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_partidas_vencedora_coerente_placar",
                table: "partidas");

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "partidas",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql("UPDATE partidas SET status = 2;");

            migrationBuilder.AlterColumn<DateTime>(
                name: "data_partida",
                table: "partidas",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddCheckConstraint(
                name: "ck_partidas_status_e_resultado",
                table: "partidas",
                sql: "((\"status\" = 1) AND \"dupla_vencedora_id\" IS NULL AND \"placar_dupla_a\" = 0 AND \"placar_dupla_b\" = 0) OR ((\"status\" = 2) AND (((\"placar_dupla_a\" = \"placar_dupla_b\") AND \"dupla_vencedora_id\" IS NULL) OR ((\"placar_dupla_a\" > \"placar_dupla_b\") AND \"dupla_vencedora_id\" = \"dupla_a_id\") OR ((\"placar_dupla_b\" > \"placar_dupla_a\") AND \"dupla_vencedora_id\" = \"dupla_b_id\")))");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_partidas_status_e_resultado",
                table: "partidas");

            migrationBuilder.DropColumn(
                name: "status",
                table: "partidas");

            migrationBuilder.Sql("UPDATE partidas SET data_partida = NOW() WHERE data_partida IS NULL;");

            migrationBuilder.AlterColumn<DateTime>(
                name: "data_partida",
                table: "partidas",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_partidas_vencedora_coerente_placar",
                table: "partidas",
                sql: "((\"placar_dupla_a\" = \"placar_dupla_b\") AND \"dupla_vencedora_id\" IS NULL) OR ((\"placar_dupla_a\" > \"placar_dupla_b\") AND \"dupla_vencedora_id\" = \"dupla_a_id\") OR ((\"placar_dupla_b\" > \"placar_dupla_a\") AND \"dupla_vencedora_id\" = \"dupla_b_id\")");
        }
    }
}
