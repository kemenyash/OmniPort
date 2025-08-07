using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Data
{
    [Table("url_conversions")]
    public class UrlConversionData
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Column("input_url")]
        public string InputUrl { get; set; }
        [Column("converted_at")]
        public DateTime ConvertedAt { get; set; }
        [Column("output_url")]
        public string OutputUrl { get; set; }

        [Column("template_map_id")]
        public int TemplateMapId { get; set; }

        [ForeignKey("TemplateMapId")]
        public TemplateMappingFieldData TemplateMap { get; set; }
    }
}
