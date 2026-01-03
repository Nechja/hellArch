using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schema.Migrations.Events
{
    /// <inheritdoc />
    public partial class AddDeadLetters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dead_letters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    event_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    event_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    source = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    original_queue = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    death_reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    body = table.Column<string>(type: "jsonb", nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dead_letters", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_dead_letters_created_at",
                table: "dead_letters",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_dead_letters_event_id",
                table: "dead_letters",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_dead_letters_event_type",
                table: "dead_letters",
                column: "event_type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dead_letters");
        }
    }
}
