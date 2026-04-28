using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    [Migration("20260401103000_AdicionarCriadoPorUsuarioNaPartida")]
    public partial class AdicionarCriadoPorUsuarioNaPartida : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "criado_por_usuario_id",
                table: "partidas",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_partidas_criado_por_usuario_id",
                table: "partidas",
                column: "criado_por_usuario_id");

            migrationBuilder.AddForeignKey(
                name: "FK_partidas_usuarios_criado_por_usuario_id",
                table: "partidas",
                column: "criado_por_usuario_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_partidas_usuarios_criado_por_usuario_id",
                table: "partidas");

            migrationBuilder.DropIndex(
                name: "IX_partidas_criado_por_usuario_id",
                table: "partidas");

            migrationBuilder.DropColumn(
                name: "criado_por_usuario_id",
                table: "partidas");
        }
    }
}
