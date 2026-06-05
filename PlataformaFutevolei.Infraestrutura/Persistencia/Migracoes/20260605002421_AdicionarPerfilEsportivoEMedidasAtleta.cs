using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class AdicionarPerfilEsportivoEMedidasAtleta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "arena_principal_id",
                table: "atletas",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "objetivo_atual",
                table: "atletas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "pe_dominante",
                table: "atletas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sexo",
                table: "atletas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tempo_pratica",
                table: "atletas",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "atletas_medidas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    atleta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    camiseta = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    regata = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    @short = table.Column<string>(name: "short", type: "character varying(10)", maxLength: 10, nullable: true),
                    sunga = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    top = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    biquini = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_atletas_medidas", x => x.id);
                    table.ForeignKey(
                        name: "FK_atletas_medidas_atletas_atleta_id",
                        column: x => x.atleta_id,
                        principalTable: "atletas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_atletas_arena_principal_id",
                table: "atletas",
                column: "arena_principal_id");

            migrationBuilder.CreateIndex(
                name: "IX_atletas_medidas_atleta_id",
                table: "atletas_medidas",
                column: "atleta_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_atletas_arenas_arena_principal_id",
                table: "atletas",
                column: "arena_principal_id",
                principalTable: "arenas",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_atletas_arenas_arena_principal_id",
                table: "atletas");

            migrationBuilder.DropTable(
                name: "atletas_medidas");

            migrationBuilder.DropIndex(
                name: "IX_atletas_arena_principal_id",
                table: "atletas");

            migrationBuilder.DropColumn(
                name: "arena_principal_id",
                table: "atletas");

            migrationBuilder.DropColumn(
                name: "objetivo_atual",
                table: "atletas");

            migrationBuilder.DropColumn(
                name: "pe_dominante",
                table: "atletas");

            migrationBuilder.DropColumn(
                name: "sexo",
                table: "atletas");

            migrationBuilder.DropColumn(
                name: "tempo_pratica",
                table: "atletas");
        }
    }
}
