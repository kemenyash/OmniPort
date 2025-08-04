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
                name: "templates",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    source_type = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "template_fields",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    template_id = table.Column<int>(type: "INTEGER", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    type = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_template_fields", x => x.id);
                    table.ForeignKey(
                        name: "FK_template_fields_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "template_mappings",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    source_template_id = table.Column<int>(type: "INTEGER", nullable: false),
                    target_template_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_template_mappings", x => x.id);
                    table.ForeignKey(
                        name: "FK_template_mappings_templates_source_template_id",
                        column: x => x.source_template_id,
                        principalTable: "templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_template_mappings_templates_target_template_id",
                        column: x => x.target_template_id,
                        principalTable: "templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "template_mapping_fields",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    mapping_id = table.Column<int>(type: "INTEGER", nullable: false),
                    target_field_id = table.Column<int>(type: "INTEGER", nullable: false),
                    source_field_id = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_template_mapping_fields", x => x.id);
                    table.ForeignKey(
                        name: "FK_template_mapping_fields_template_fields_source_field_id",
                        column: x => x.source_field_id,
                        principalTable: "template_fields",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_template_mapping_fields_template_fields_target_field_id",
                        column: x => x.target_field_id,
                        principalTable: "template_fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_template_mapping_fields_template_mappings_mapping_id",
                        column: x => x.mapping_id,
                        principalTable: "template_mappings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_template_fields_template_id",
                table: "template_fields",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "IX_template_mapping_fields_mapping_id",
                table: "template_mapping_fields",
                column: "mapping_id");

            migrationBuilder.CreateIndex(
                name: "IX_template_mapping_fields_source_field_id",
                table: "template_mapping_fields",
                column: "source_field_id");

            migrationBuilder.CreateIndex(
                name: "IX_template_mapping_fields_target_field_id",
                table: "template_mapping_fields",
                column: "target_field_id");

            migrationBuilder.CreateIndex(
                name: "IX_template_mappings_source_template_id",
                table: "template_mappings",
                column: "source_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_template_mappings_target_template_id",
                table: "template_mappings",
                column: "target_template_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "template_mapping_fields");

            migrationBuilder.DropTable(
                name: "template_fields");

            migrationBuilder.DropTable(
                name: "template_mappings");

            migrationBuilder.DropTable(
                name: "templates");
        }
    }
}
