using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmniPort.Data
{
    [Table("file_conversion_history")]
    public class FileConversionHistoryData
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required, Column("converted_at")]
        public DateTime ConvertedAt { get; set; }

        [Required, Column("file_name")]
        public string FileName { get; set; } = null!;

        [Required, Column("output_url")]
        public string OutputUrl { get; set; } = null!;

        [Required, Column("mapping_template_id")]
        public int MappingTemplateId { get; set; }

        [ForeignKey(nameof(MappingTemplateId))]
        public MappingTemplateData MappingTemplate { get; set; } = null!;
    }
}
