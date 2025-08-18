using OmniPort.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Data
{
    [Table("fields")]
    public class FieldData
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required, Column("basic_template_id")]
        public int TemplateSourceId { get; set; }

        [Required, Column("field_type")]
        public FieldDataType Type { get; set; }

        [Required, Column("name")]
        public string Name { get; set; } = null!;

        [ForeignKey(nameof(TemplateSourceId))]
        public BasicTemplateData TemplateSource { get; set; } = null!;
    }
}
