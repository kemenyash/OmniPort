using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Data.New
{
    public class MappingFieldData
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required, Column("mapping_template_id")]
        public int MappingTemplateId { get; set; }

        [Required, Column("source_field_id")]
        public int SourceFieldId { get; set; }

        [Required, Column("target_field_id")]
        public int TargetFieldId { get; set; }

        [ForeignKey(nameof(MappingTemplateId))]
        public MappingTemplateData MappingTemplate { get; set; } = null!;

        [ForeignKey(nameof(SourceFieldId))]
        public FieldData SourceField { get; set; } = null!;

        [ForeignKey(nameof(TargetFieldId))]
        public FieldData TargetField { get; set; } = null!;
    }
}
