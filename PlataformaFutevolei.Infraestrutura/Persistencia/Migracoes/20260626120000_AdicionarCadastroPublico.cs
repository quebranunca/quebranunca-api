using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes;

[DbContext(typeof(PlataformaFutevoleiDbContext))]
[Migration("20260626120000_AdicionarCadastroPublico")]
public partial class AdicionarCadastroPublico : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "email_confirmado_em_utc",
            table: "usuarios",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "cadastro_completo_em_utc",
            table: "usuarios",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "consentimento_marketing_em_utc",
            table: "usuarios",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "revogou_marketing_em_utc",
            table: "usuarios",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE usuarios
            SET email_confirmado_em_utc = COALESCE(email_confirmado_em_utc, senha_definida_em_utc, data_criacao),
                cadastro_completo_em_utc = COALESCE(cadastro_completo_em_utc, data_criacao)
            WHERE ativo = true
              AND dados_anonimizados = false
              AND email IS NOT NULL
              AND btrim(email) <> '';
            """);

        migrationBuilder.AddColumn<string>(
            name: "versao_termos_uso",
            table: "usuarios_consentimentos_lgpd",
            type: "character varying(50)",
            maxLength: 50,
            nullable: false,
            defaultValue: "2026-05-18");

        migrationBuilder.AddColumn<DateTime>(
            name: "declarou_maioridade_em_utc",
            table: "usuarios_consentimentos_lgpd",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "aceitou_marketing",
            table: "usuarios_consentimentos_lgpd",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTime>(
            name: "consentimento_marketing_em_utc",
            table: "usuarios_consentimentos_lgpd",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "origem",
            table: "usuarios_consentimentos_lgpd",
            type: "character varying(80)",
            maxLength: 80,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "codigos_acesso_email",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                email_normalizado = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                codigo_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                finalidade = table.Column<int>(type: "integer", nullable: false),
                expira_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                tentativas = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                consumido_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ultimo_envio_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                cadastro_token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                cadastro_token_expira_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_codigos_acesso_email", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_usuarios_consentimentos_lgpd_usuario_id_versao_termos_uso",
            table: "usuarios_consentimentos_lgpd",
            columns: new[] { "usuario_id", "versao_termos_uso" });

        migrationBuilder.CreateIndex(
            name: "IX_codigos_acesso_email_cadastro_token_hash",
            table: "codigos_acesso_email",
            column: "cadastro_token_hash");

        migrationBuilder.CreateIndex(
            name: "IX_codigos_acesso_email_email_normalizado_finalidade_consumido~",
            table: "codigos_acesso_email",
            columns: new[] { "email_normalizado", "finalidade", "consumido_em_utc" });

        migrationBuilder.CreateIndex(
            name: "IX_codigos_acesso_email_expira_em_utc",
            table: "codigos_acesso_email",
            column: "expira_em_utc");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "codigos_acesso_email");

        migrationBuilder.DropIndex(
            name: "IX_usuarios_consentimentos_lgpd_usuario_id_versao_termos_uso",
            table: "usuarios_consentimentos_lgpd");

        migrationBuilder.DropColumn(
            name: "email_confirmado_em_utc",
            table: "usuarios");

        migrationBuilder.DropColumn(
            name: "cadastro_completo_em_utc",
            table: "usuarios");

        migrationBuilder.DropColumn(
            name: "consentimento_marketing_em_utc",
            table: "usuarios");

        migrationBuilder.DropColumn(
            name: "revogou_marketing_em_utc",
            table: "usuarios");

        migrationBuilder.DropColumn(
            name: "versao_termos_uso",
            table: "usuarios_consentimentos_lgpd");

        migrationBuilder.DropColumn(
            name: "declarou_maioridade_em_utc",
            table: "usuarios_consentimentos_lgpd");

        migrationBuilder.DropColumn(
            name: "aceitou_marketing",
            table: "usuarios_consentimentos_lgpd");

        migrationBuilder.DropColumn(
            name: "consentimento_marketing_em_utc",
            table: "usuarios_consentimentos_lgpd");

        migrationBuilder.DropColumn(
            name: "origem",
            table: "usuarios_consentimentos_lgpd");
    }
}
