using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniPort.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedTemplateMapData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "template_map_id",
                table: "url_conversions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "template_map_id",
                table: "file_conversions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_url_conversions_template_map_id",
                table: "url_conversions",
                column: "template_map_id");

            migrationBuilder.CreateIndex(
                name: "IX_file_conversions_template_map_id",
                table: "file_conversions",
                column: "template_map_id");

            migrationBuilder.AddForeignKey(
                name: "FK_file_conversions_template_mapping_fields_template_map_id",
                table: "file_conversions",
                column: "template_map_id",
                principalTable: "template_mapping_fields",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_url_conversions_template_mapping_fields_template_map_id",
                table: "url_conversions",
                column: "template_map_id",
                principalTable: "template_mapping_fields",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_file_conversions_template_mapping_fields_template_map_id",
                table: "file_conversions");

            migrationBuilder.DropForeignKey(
                name: "FK_url_conversions_template_mapping_fields_template_map_id",
                table: "url_conversions");

            migrationBuilder.DropIndex(
                name: "IX_url_conversions_template_map_id",
                table: "url_conversions");

            migrationBuilder.DropIndex(
                name: "IX_file_conversions_template_map_id",
                table: "file_conversions");

            migrationBuilder.DropColumn(
                name: "template_map_id",
                table: "url_conversions");

            migrationBuilder.DropColumn(
                name: "template_map_id",
                table: "file_conversions");
        }
    }
}
