using OmniPort.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Data.New
{
    //Basic Templates
    [Table("basic_templates")]
    public class BasicTemplateData
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required, Column("name")]
        public string Name { get; set; } = null!;

        [Required, Column("source_type")]
        public SourceType SourceType { get; set; }

        public ICollection<FieldData> Fields { get; set; } = new List<FieldData>();
        public ICollection<MappingTemplateData> AsSourceMappings { get; set; } = new List<MappingTemplateData>();
        public ICollection<MappingTemplateData> AsTargetMappings { get; set; } = new List<MappingTemplateData>();
    }
}
