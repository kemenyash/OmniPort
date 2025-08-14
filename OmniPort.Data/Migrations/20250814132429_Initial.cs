using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniPort.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "basic_templates",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    source_type = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_basic_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "url_file_getting",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    url = table.Column<string>(type: "TEXT", nullable: false),
                    check_interval_min = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_url_file_getting", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fields",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    basic_template_id = table.Column<int>(type: "INTEGER", nullable: false),
                    field_type = table.Column<int>(type: "INTEGER", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fields", x => x.id);
                    table.ForeignKey(
                        name: "FK_fields_basic_templates_basic_template_id",
                        column: x => x.basic_template_id,
                        principalTable: "basic_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "template_mapping",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    source_template_id = table.Column<int>(type: "INTEGER", nullable: false),
                    target_template_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_template_mapping", x => x.id);
                    table.ForeignKey(
                        name: "FK_template_mapping_basic_templates_source_template_id",
                        column: x => x.source_template_id,
                        principalTable: "basic_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_template_mapping_basic_templates_target_template_id",
                        column: x => x.target_template_id,
                        principalTable: "basic_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "file_conversion_history",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    converted_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    file_name = table.Column<string>(type: "TEXT", nullable: false),
                    output_url = table.Column<string>(type: "TEXT", nullable: false),
                    mapping_template_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_conversion_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_file_conversion_history_template_mapping_mapping_template_id",
                        column: x => x.mapping_template_id,
                        principalTable: "template_mapping",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mapping_fields",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    mapping_template_id = table.Column<int>(type: "INTEGER", nullable: false),
                    source_field_id = table.Column<int>(type: "INTEGER", nullable: false),
                    target_field_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mapping_fields", x => x.id);
                    table.ForeignKey(
                        name: "FK_mapping_fields_fields_source_field_id",
                        column: x => x.source_field_id,
                        principalTable: "fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_mapping_fields_fields_target_field_id",
                        column: x => x.target_field_id,
                        principalTable: "fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_mapping_fields_template_mapping_mapping_template_id",
                        column: x => x.mapping_template_id,
                        principalTable: "template_mapping",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "url_conversion_history",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    converted_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    input_url = table.Column<string>(type: "TEXT", nullable: false),
                    output_url = table.Column<string>(type: "TEXT", nullable: false),
                    mapping_template_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_url_conversion_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_url_conversion_history_template_mapping_mapping_template_id",
                        column: x => x.mapping_template_id,
                        principalTable: "template_mapping",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_basic_templates_name",
                table: "basic_templates",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_fields_basic_template_id_name",
                table: "fields",
                columns: new[] { "basic_template_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_file_conversion_history_mapping_template_id",
                table: "file_conversion_history",
                column: "mapping_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_mapping_fields_mapping_template_id_source_field_id_target_field_id",
                table: "mapping_fields",
                columns: new[] { "mapping_template_id", "source_field_id", "target_field_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mapping_fields_source_field_id",
                table: "mapping_fields",
                column: "source_field_id");

            migrationBuilder.CreateIndex(
                name: "IX_mapping_fields_target_field_id",
                table: "mapping_fields",
                column: "target_field_id");

            migrationBuilder.CreateIndex(
                name: "IX_template_mapping_name",
                table: "template_mapping",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_template_mapping_source_template_id",
                table: "template_mapping",
                column: "source_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_template_mapping_target_template_id",
                table: "template_mapping",
                column: "target_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_url_conversion_history_mapping_template_id",
                table: "url_conversion_history",
                column: "mapping_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_url_file_getting_url",
                table: "url_file_getting",
                column: "url",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "file_conversion_history");

            migrationBuilder.DropTable(
                name: "mapping_fields");

            migrationBuilder.DropTable(
                name: "url_conversion_history");

            migrationBuilder.DropTable(
                name: "url_file_getting");

            migrationBuilder.DropTable(
                name: "fields");

            migrationBuilder.DropTable(
                name: "template_mapping");

            migrationBuilder.DropTable(
                name: "basic_templates");
        }
    }
}
