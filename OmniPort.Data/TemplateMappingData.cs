using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Data
{
    [Table("template_mappings")]
    public class TemplateMappingData
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("source_template_id")]
        public int SourceTemplateId { get; set; }

        [Column("target_template_id")]
        public int TargetTemplateId { get; set; }

        [ForeignKey("SourceTemplateId")]
        public TemplateData SourceTemplate { get; set; } = null!;

        [ForeignKey("TargetTemplateId")]
        public TemplateData TargetTemplate { get; set; } = null!;
    }
}
