using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppAlumnos.Data.Migrations
{
    /// <inheritdoc />
    public partial class _003_AgregarCarreraYAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CarreraId",
                table: "Materias",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCreacion",
                table: "Materias",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaModificacion",
                table: "Materias",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioCreacionId",
                table: "Materias",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioModificacionId",
                table: "Materias",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Carreras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DuracionAnios = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioCreacionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioModificacionId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carreras", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Materias_CarreraId",
                table: "Materias",
                column: "CarreraId");

            migrationBuilder.AddForeignKey(
                name: "FK_Materias_Carreras_CarreraId",
                table: "Materias",
                column: "CarreraId",
                principalTable: "Carreras",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Materias_Carreras_CarreraId",
                table: "Materias");

            migrationBuilder.DropTable(
                name: "Carreras");

            migrationBuilder.DropIndex(
                name: "IX_Materias_CarreraId",
                table: "Materias");

            migrationBuilder.DropColumn(
                name: "CarreraId",
                table: "Materias");

            migrationBuilder.DropColumn(
                name: "FechaCreacion",
                table: "Materias");

            migrationBuilder.DropColumn(
                name: "FechaModificacion",
                table: "Materias");

            migrationBuilder.DropColumn(
                name: "UsuarioCreacionId",
                table: "Materias");

            migrationBuilder.DropColumn(
                name: "UsuarioModificacionId",
                table: "Materias");
        }
    }
}
