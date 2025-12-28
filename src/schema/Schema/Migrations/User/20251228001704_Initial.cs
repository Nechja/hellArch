using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schema.Migrations.User
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    auth_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    bio = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    theme = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    timezone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email_notifications = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_preferences", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_preferences_user_profiles_user_profile_id",
                        column: x => x.user_profile_id,
                        principalTable: "user_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_preferences_user_profile_id",
                table: "user_preferences",
                column: "user_profile_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_profiles_auth_user_id",
                table: "user_profiles",
                column: "auth_user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_preferences");

            migrationBuilder.DropTable(
                name: "user_profiles");
        }
    }
}
