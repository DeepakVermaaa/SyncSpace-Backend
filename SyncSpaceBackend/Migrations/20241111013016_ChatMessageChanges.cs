using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SyncSpaceBackend.Migrations
{
    /// <inheritdoc />
    public partial class ChatMessageChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_users_SenderId1",
                table: "ChatMessages");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_SenderId1",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "SenderId1",
                table: "ChatMessages");

            migrationBuilder.AlterColumn<int>(
                name: "SenderId",
                table: "ChatMessages",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SenderId",
                table: "ChatMessages",
                column: "SenderId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_users_SenderId",
                table: "ChatMessages",
                column: "SenderId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_users_SenderId",
                table: "ChatMessages");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_SenderId",
                table: "ChatMessages");

            migrationBuilder.AlterColumn<string>(
                name: "SenderId",
                table: "ChatMessages",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "SenderId1",
                table: "ChatMessages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SenderId1",
                table: "ChatMessages",
                column: "SenderId1");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_users_SenderId1",
                table: "ChatMessages",
                column: "SenderId1",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
