using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class AdicionarTelefoneNormalizadoAtleta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "telefone_normalizado",
                table: "atletas",
                type: "character varying(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE atletas
                SET telefone_normalizado = CASE
                    WHEN length(regexp_replace(telefone, '[^0-9]', '', 'g')) IN (12, 13)
                         AND regexp_replace(telefone, '[^0-9]', '', 'g') LIKE '55%'
                        THEN substring(regexp_replace(telefone, '[^0-9]', '', 'g') FROM 3)
                    WHEN length(regexp_replace(telefone, '[^0-9]', '', 'g')) IN (10, 11)
                        THEN regexp_replace(telefone, '[^0-9]', '', 'g')
                    ELSE NULL
                END
                WHERE telefone IS NOT NULL;

                WITH duplicados AS (
                    SELECT id,
                           row_number() OVER (
                               PARTITION BY telefone_normalizado
                               ORDER BY data_criacao, id) AS ordem
                    FROM atletas
                    WHERE telefone_normalizado IS NOT NULL
                )
                UPDATE atletas a
                SET telefone_normalizado = NULL
                FROM duplicados d
                WHERE a.id = d.id AND d.ordem > 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_atletas_telefone_normalizado",
                table: "atletas",
                column: "telefone_normalizado",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_atletas_telefone_normalizado",
                table: "atletas");

            migrationBuilder.DropColumn(
                name: "telefone_normalizado",
                table: "atletas");
        }
    }
}
