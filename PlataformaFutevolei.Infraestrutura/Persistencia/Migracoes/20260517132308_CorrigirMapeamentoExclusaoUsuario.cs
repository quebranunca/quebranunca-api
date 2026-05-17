using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class CorrigirMapeamentoExclusaoUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Hotfix de produção:
            // migration intencionalmente vazia para alinhar o ModelSnapshot
            // sem alterar a estrutura atual do banco.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Hotfix de produção:
            // não reverter estrutura de banco nesta migration.
        }
    }
}