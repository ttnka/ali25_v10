using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ali25_V10.Data.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddNivelToApplicationUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Nivel",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Z900_Bitacora",
                columns: table => new
                {
                    BitacoraId = table.Column<string>(type: "varchar(65)", maxLength: 65, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UserId = table.Column<string>(type: "varchar(65)", maxLength: 65, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Desc = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrgId = table.Column<string>(type: "varchar(65)", maxLength: 65, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Z900_Bitacora", x => x.BitacoraId);
                    table.ForeignKey(
                        name: "FK_Z900_Bitacora_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Z900_Bitacora_Organizaciones_OrgId",
                        column: x => x.OrgId,
                        principalTable: "Organizaciones",
                        principalColumn: "OrgId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Z900_Bitacora_OrgId",
                table: "Z900_Bitacora",
                column: "OrgId");

            migrationBuilder.CreateIndex(
                name: "IX_Z900_Bitacora_UserId",
                table: "Z900_Bitacora",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Z900_Bitacora");

            migrationBuilder.DropColumn(
                name: "Nivel",
                table: "AspNetUsers");
        }
    }
}
