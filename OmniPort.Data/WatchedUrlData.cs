using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Data
{
    [Table("watched_urls")]
    public class WatchedUrlData
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Column("url")]
        public string Url { get; set; }
        [Column("interval_minutes")]
        public int IntervalMinutes { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
