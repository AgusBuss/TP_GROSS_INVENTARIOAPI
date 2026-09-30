using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppAlumnos.Data.Migrations
{
    /// <inheritdoc />
    public partial class _005_RestrictCarreraMateria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Materias_Carreras_CarreraId",
                table: "Materias");

            migrationBuilder.AddForeignKey(
                name: "FK_Materias_Carreras_CarreraId",
                table: "Materias",
                column: "CarreraId",
                principalTable: "Carreras",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Materias_Carreras_CarreraId",
                table: "Materias");

            migrationBuilder.AddForeignKey(
                name: "FK_Materias_Carreras_CarreraId",
                table: "Materias",
                column: "CarreraId",
                principalTable: "Carreras",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
