using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schema.Migrations.Events
{
    /// <inheritdoc />
    public partial class AddWebhookFormat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "format",
                table: "webhooks",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Raw");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "format",
                table: "webhooks");
        }
    }
}
