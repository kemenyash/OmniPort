using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniPort.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnableCascadesForTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_file_conversion_history_template_mapping_mapping_template_id",
                table: "file_conversion_history");

            migrationBuilder.DropForeignKey(
                name: "FK_mapping_fields_fields_source_field_id",
                table: "mapping_fields");

            migrationBuilder.DropForeignKey(
                name: "FK_mapping_fields_fields_target_field_id",
                table: "mapping_fields");

            migrationBuilder.DropForeignKey(
                name: "FK_template_mapping_basic_templates_source_template_id",
                table: "template_mapping");

            migrationBuilder.DropForeignKey(
                name: "FK_template_mapping_basic_templates_target_template_id",
                table: "template_mapping");

            migrationBuilder.DropForeignKey(
                name: "FK_url_conversion_history_template_mapping_mapping_template_id",
                table: "url_conversion_history");

            migrationBuilder.AddForeignKey(
                name: "FK_file_conversion_history_template_mapping_mapping_template_id",
                table: "file_conversion_history",
                column: "mapping_template_id",
                principalTable: "template_mapping",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_mapping_fields_fields_source_field_id",
                table: "mapping_fields",
                column: "source_field_id",
                principalTable: "fields",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_mapping_fields_fields_target_field_id",
                table: "mapping_fields",
                column: "target_field_id",
                principalTable: "fields",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_template_mapping_basic_templates_source_template_id",
                table: "template_mapping",
                column: "source_template_id",
                principalTable: "basic_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_template_mapping_basic_templates_target_template_id",
                table: "template_mapping",
                column: "target_template_id",
                principalTable: "basic_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_url_conversion_history_template_mapping_mapping_template_id",
                table: "url_conversion_history",
                column: "mapping_template_id",
                principalTable: "template_mapping",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_file_conversion_history_template_mapping_mapping_template_id",
                table: "file_conversion_history");

            migrationBuilder.DropForeignKey(
                name: "FK_mapping_fields_fields_source_field_id",
                table: "mapping_fields");

            migrationBuilder.DropForeignKey(
                name: "FK_mapping_fields_fields_target_field_id",
                table: "mapping_fields");

            migrationBuilder.DropForeignKey(
                name: "FK_template_mapping_basic_templates_source_template_id",
                table: "template_mapping");

            migrationBuilder.DropForeignKey(
                name: "FK_template_mapping_basic_templates_target_template_id",
                table: "template_mapping");

            migrationBuilder.DropForeignKey(
                name: "FK_url_conversion_history_template_mapping_mapping_template_id",
                table: "url_conversion_history");

            migrationBuilder.AddForeignKey(
                name: "FK_file_conversion_history_template_mapping_mapping_template_id",
                table: "file_conversion_history",
                column: "mapping_template_id",
                principalTable: "template_mapping",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_mapping_fields_fields_source_field_id",
                table: "mapping_fields",
                column: "source_field_id",
                principalTable: "fields",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_mapping_fields_fields_target_field_id",
                table: "mapping_fields",
                column: "target_field_id",
                principalTable: "fields",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_template_mapping_basic_templates_source_template_id",
                table: "template_mapping",
                column: "source_template_id",
                principalTable: "basic_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_template_mapping_basic_templates_target_template_id",
                table: "template_mapping",
                column: "target_template_id",
                principalTable: "basic_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_url_conversion_history_template_mapping_mapping_template_id",
                table: "url_conversion_history",
                column: "mapping_template_id",
                principalTable: "template_mapping",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
