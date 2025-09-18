using OmniPort.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        [Column("parent_field_id")]
        public int? ParentFieldId { get; set; }

        [Required, Column("is_array_item")]
        public bool IsArrayItem { get; set; }

        [Column("item_type")]
        public FieldDataType? ItemType { get; set; }

        [ForeignKey(nameof(TemplateSourceId))]
        public BasicTemplateData TemplateSource { get; set; } = null!;

        [ForeignKey(nameof(ParentFieldId))]
        [InverseProperty(nameof(Children))]
        public FieldData? ParentField { get; set; }

        [InverseProperty(nameof(ParentField))]
        public ICollection<FieldData> Children { get; set; } = new List<FieldData>();

    }
}
