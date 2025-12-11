using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmniPort.Data
{
    [Table("url_file_getting")]
    public class UrlFileGettingData
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required, Column("url")]
        public string Url { get; set; } = null!;

        [Required, Column("check_interval_min")]
        public int CheckIntervalMinutes { get; set; }

        [Required, Column("mapping_template_id")]
        public int MappingTemplateId { get; set; }

        [ForeignKey(nameof(MappingTemplateId))]
        public MappingTemplateData MappingTemplate { get; set; } = null!;
    }
}
