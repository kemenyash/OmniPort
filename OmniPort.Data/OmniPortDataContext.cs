using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Data
{
    public class OmniPortDataContext : DbContext
    {
        public DbSet<TemplateData> Templates { get; set; }
        public DbSet<TemplateFieldData> TemplateFields { get; set; }
        public DbSet<TemplateMappingData> TemplateMappings { get; set; }
        public DbSet<TemplateMappingFieldData> TemplateMappingFields { get; set; }

        public OmniPortDataContext(DbContextOptions<OmniPortDataContext> options)
            : base(options)
        {
        }
    }
}
