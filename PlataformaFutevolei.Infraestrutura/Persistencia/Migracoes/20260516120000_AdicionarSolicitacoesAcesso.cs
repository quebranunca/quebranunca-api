using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Dominio.Enums;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes;

[DbContext(typeof(PlataformaFutevoleiDbContext))]
[Migration("20260516120000_AdicionarSolicitacoesAcesso")]
public partial class AdicionarSolicitacoesAcesso : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "solicitacoes_acesso",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                status = table.Column<int>(
                    type: "integer",
                    nullable: false,
                    defaultValue: StatusSolicitacaoAcesso.Pendente),
                data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_solicitacoes_acesso", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_solicitacoes_acesso_email",
            table: "solicitacoes_acesso",
            column: "email",
            unique: true,
            filter: "status = 1");

        migrationBuilder.CreateIndex(
            name: "ix_solicitacoes_acesso_email_status",
            table: "solicitacoes_acesso",
            columns: new[] { "email", "status" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "solicitacoes_acesso");
    }
}
