using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Data
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
        public string InputUrl { get; set; }

        [Required, Column("output_url")]
        public string OutputUrl { get; set; }

        [Required, Column("mapping_template_id")]
        public int MappingTemplateId { get; set; }

        [ForeignKey(nameof(MappingTemplateId))]
        public MappingTemplateData MappingTemplate { get; set; } = null!;

    }
}
