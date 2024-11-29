using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SyncSpaceBackend.Migrations
{
    /// <inheritdoc />
    public partial class ProjectGroupChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ProjectGroups",
                type: "varchar(255)",
                nullable: false,
                defaultValue: "Planning",
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldDefaultValue: "Active")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ProjectGroups",
                type: "varchar(255)",
                nullable: false,
                defaultValue: "Active",
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldDefaultValue: "Planning")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
