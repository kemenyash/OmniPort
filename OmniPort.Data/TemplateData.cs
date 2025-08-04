using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Data
{
    [Table("templates")]
    public class TemplateData
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = null!;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("source_type")]
        public string SourceType { get; set; } = null!;

        [InverseProperty("SourceTemplate")]
        public ICollection<TemplateMappingData> SourceMappings { get; set; }

        [InverseProperty("TargetTemplate")]
        public ICollection<TemplateMappingData> TargetMappings { get; set; }

        public ICollection<TemplateFieldData> Fields { get; set; }
    }
}
