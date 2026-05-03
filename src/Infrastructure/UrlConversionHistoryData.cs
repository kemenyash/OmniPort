using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infrastructure
{
    [Table("url_conversion_history")]
    public class UrlConversionHistoryData
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required, Column("converted_at")]
        public DateTime ConvertedAt { get; set; }

        [Required, Column("input_url")]
        public string InputUrl { get; set; } = null!;

        [Required, Column("output_url")]
        public string OutputUrl { get; set; } = null!;

        [Required, Column("mapping_template_id")]
        public int MappingTemplateId { get; set; }

        [ForeignKey(nameof(MappingTemplateId))]
        public MappingTemplateData MappingTemplate { get; set; } = null!;

    }
}
