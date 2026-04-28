using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes;

[DbContext(typeof(PlataformaFutevoleiDbContext))]
[Migration("20260330190000_AdicionarConvitesCadastroFechado")]
public partial class AdicionarConvitesCadastroFechado : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "convites_cadastro",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                telefone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                token = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                perfil_destino = table.Column<int>(type: "integer", nullable: false),
                expira_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                usado_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ativo = table.Column<bool>(type: "boolean", nullable: false),
                criado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                canal_envio = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_convites_cadastro", x => x.id);
                table.ForeignKey(
                    name: "FK_convites_cadastro_usuarios_criado_por_usuario_id",
                    column: x => x.criado_por_usuario_id,
                    principalTable: "usuarios",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_convites_cadastro_ativo_expira_em_utc",
            table: "convites_cadastro",
            columns: new[] { "ativo", "expira_em_utc" });

        migrationBuilder.CreateIndex(
            name: "IX_convites_cadastro_criado_por_usuario_id",
            table: "convites_cadastro",
            column: "criado_por_usuario_id");

        migrationBuilder.CreateIndex(
            name: "IX_convites_cadastro_email",
            table: "convites_cadastro",
            column: "email");

        migrationBuilder.CreateIndex(
            name: "IX_convites_cadastro_token",
            table: "convites_cadastro",
            column: "token",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "convites_cadastro");
    }
}
