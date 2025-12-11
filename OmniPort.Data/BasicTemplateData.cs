using OmniPort.Core.Enums;
using OmniPort.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
    [InverseProperty(nameof(MappingTemplateData.SourceTemplate))]
    public ICollection<MappingTemplateData> AsSourceMappings { get; set; } = new List<MappingTemplateData>();

    [InverseProperty(nameof(MappingTemplateData.TargetTemplate))]
    public ICollection<MappingTemplateData> AsTargetMappings { get; set; } = new List<MappingTemplateData>();
}
