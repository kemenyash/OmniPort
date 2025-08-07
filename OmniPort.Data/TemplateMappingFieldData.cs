using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Data
{
    [Table("template_mapping_fields")]
    public class TemplateMappingFieldData
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [ForeignKey("Mapping")]
        [Column("mapping_id")]
        public int MappingId { get; set; }

        [ForeignKey("TargetField")]
        [Column("target_field_id")]
        public int TargetFieldId { get; set; }

        [ForeignKey("SourceField")]
        [Column("source_field_id")]
        public int? SourceFieldId { get; set; }

        public TemplateMappingData Mapping { get; set; }
        public TemplateFieldData TargetField { get; set; }
        public TemplateFieldData? SourceField { get; set; }
    }
}
