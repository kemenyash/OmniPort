using Microsoft.VisualBasic.FileIO;
using OmniPort.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Data
{
    [Table("template_fields")]
    public class TemplateFieldData
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [ForeignKey("Template")]
        [Column("template_id")]
        public int TemplateId { get; set; }

        [Required]
        [Column("name")]
        public string Name { get; set; } = null!;

        [Required]
        [Column("type")]
        public FieldDataType Type { get; set; }

        public TemplateData Template { get; set; } = null!;
    }
}
