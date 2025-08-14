using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Data.New
{
    //Transforming Templates
    [Table("template_mapping")]
    public class MappingTemplateData
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required, Column("name")]
        public string Name { get; set; } = null!;

        [Required, Column("source_template_id")]
        public int SourceTemplateId { get; set; }

        [Required, Column("target_template_id")]
        public int TargetTemplateId { get; set; }

        [ForeignKey(nameof(SourceTemplateId))]
        public BasicTemplateData SourceTemplate { get; set; } = null!;

        [ForeignKey(nameof(TargetTemplateId))]
        public BasicTemplateData TargetTemplate { get; set; } = null!;

        public ICollection<MappingFieldData> MappingFields { get; set; } = new List<MappingFieldData>();
        public ICollection<FileConversionHistoryData> FileConversions { get; set; } = new List<FileConversionHistoryData>();
        public ICollection<UrlConversionHistoryData> UrlConversions { get; set; } = new List<UrlConversionHistoryData>();

    }
}
