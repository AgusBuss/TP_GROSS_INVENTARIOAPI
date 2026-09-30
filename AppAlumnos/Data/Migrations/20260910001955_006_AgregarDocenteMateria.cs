using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppAlumnos.Data.Migrations
{
    /// <inheritdoc />
    public partial class _006_AgregarDocenteMateria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocentesMaterias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocenteId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MateriaId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioCreacionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioModificacionId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocentesMaterias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocentesMaterias_AspNetUsers_DocenteId",
                        column: x => x.DocenteId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocentesMaterias_Materias_MateriaId",
                        column: x => x.MateriaId,
                        principalTable: "Materias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocentesMaterias_DocenteId_MateriaId",
                table: "DocentesMaterias",
                columns: new[] { "DocenteId", "MateriaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocentesMaterias_MateriaId",
                table: "DocentesMaterias",
                column: "MateriaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocentesMaterias");
        }
    }
}
