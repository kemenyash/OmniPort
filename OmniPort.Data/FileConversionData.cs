using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Data
{
    [Table("file_conversions")]
    public class FileConversionData
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Column("file_name")]
        public string FileName { get; set; }
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
