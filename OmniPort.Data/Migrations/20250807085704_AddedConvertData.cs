using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniPort.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedConvertData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "file_conversions",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    file_name = table.Column<string>(type: "TEXT", nullable: false),
                    converted_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    output_url = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_conversions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "url_conversions",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    input_url = table.Column<string>(type: "TEXT", nullable: false),
                    converted_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    output_url = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_url_conversions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "watched_urls",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    url = table.Column<string>(type: "TEXT", nullable: false),
                    interval_minutes = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_watched_urls", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "file_conversions");

            migrationBuilder.DropTable(
                name: "url_conversions");

            migrationBuilder.DropTable(
                name: "watched_urls");
        }
    }
}
