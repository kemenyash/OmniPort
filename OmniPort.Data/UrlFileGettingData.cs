using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Data
{
    [Table("url_file_getting")]
    public class UrlFileGettingData
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required, Column("url")]
        public string Url { get; set; } = null!;

        [Required, Column("check_interval_min")]
        public int CheckIntervalMinutes { get; set; }

    }
}
