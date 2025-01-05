using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SyncSpaceBackend.Migrations
{
    /// <inheritdoc />
    public partial class FixDocumentProjectRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropForeignKey(
            //    name: "FK_Documents_ProjectGroups_ProjectGroupId",
            //    table: "Documents");

            //migrationBuilder.DropForeignKey(
            //    name: "FK_Documents_ProjectGroups_ProjectId",
            //    table: "Documents");

            //migrationBuilder.DropIndex(
            //    name: "IX_Documents_ProjectId",
            //    table: "Documents");

            //migrationBuilder.DropColumn(
            //    name: "ProjectId",
            //    table: "Documents");

            migrationBuilder.AlterColumn<int>(
                name: "ProjectGroupId",
                table: "Documents",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_ProjectGroups_ProjectGroupId",
                table: "Documents",
                column: "ProjectGroupId",
                principalTable: "ProjectGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_ProjectGroups_ProjectGroupId",
                table: "Documents");

            //migrationBuilder.AlterColumn<int>(
            //    name: "ProjectGroupId",
            //    table: "Documents",
            //    type: "int",
            //    nullable: true,
            //    oldClrType: typeof(int),
            //    oldType: "int");

            //migrationBuilder.AddColumn<int>(
            //    name: "ProjectId",
            //    table: "Documents",
            //    type: "int",
            //    nullable: false,
            //    defaultValue: 0);

            //migrationBuilder.CreateIndex(
            //    name: "IX_Documents_ProjectId",
            //    table: "Documents",
            //    column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_ProjectGroups_ProjectGroupId",
                table: "Documents",
                column: "ProjectGroupId",
                principalTable: "ProjectGroups",
                principalColumn: "Id");

            //migrationBuilder.AddForeignKey(
            //    name: "FK_Documents_ProjectGroups_ProjectId",
            //    table: "Documents",
            //    column: "ProjectId",
            //    principalTable: "ProjectGroups",
            //    principalColumn: "Id",
            //    onDelete: ReferentialAction.Cascade);
        }
    }
}
