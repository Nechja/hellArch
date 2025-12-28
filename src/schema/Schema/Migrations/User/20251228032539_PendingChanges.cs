using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schema.Migrations.User
{
    /// <inheritdoc />
    public partial class PendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "user_profiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "in_app_notifications",
                table: "user_preferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "push_notifications",
                table: "user_preferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "email",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "in_app_notifications",
                table: "user_preferences");

            migrationBuilder.DropColumn(
                name: "push_notifications",
                table: "user_preferences");
        }
    }
}
