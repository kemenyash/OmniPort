using Microsoft.EntityFrameworkCore;
using OmniPort.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Data
{
    public class OmniPortDataContext : DbContext
    {
        public OmniPortDataContext(DbContextOptions<OmniPortDataContext> options) : base(options) { }

        public DbSet<BasicTemplateData> BasicTemplates => Set<BasicTemplateData>();
        public DbSet<FieldData> Fields => Set<FieldData>();
        public DbSet<MappingTemplateData> MappingTemplates => Set<MappingTemplateData>();
        public DbSet<MappingFieldData> MappingFields => Set<MappingFieldData>();
        public DbSet<FileConversionHistoryData> FileConversionHistory => Set<FileConversionHistoryData>();
        public DbSet<UrlConversionHistoryData> UrlConversionHistory => Set<UrlConversionHistoryData>();
        public DbSet<UrlFileGettingData> UrlFileGetting => Set<UrlFileGettingData>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BasicTemplateData>()
                .HasIndex(x => x.Name).IsUnique(false);

            modelBuilder.Entity<BasicTemplateData>()
                .HasMany(t => t.Fields)
                .WithOne(f => f.TemplateSource)
                .HasForeignKey(f => f.TemplateSourceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FieldData>()
                .HasOne(f => f.ParentField)
                .WithMany(p => p.Children)
                .HasForeignKey(f => f.ParentFieldId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FieldData>()
                .HasIndex(f => new { f.TemplateSourceId, f.ParentFieldId, f.IsArrayItem, f.Name })
                .IsUnique();

            modelBuilder.Entity<MappingTemplateData>()
                .HasIndex(m => m.Name).IsUnique(false);

            modelBuilder.Entity<MappingTemplateData>()
                .HasMany(m => m.MappingFields)
                .WithOne(f => f.MappingTemplate)
                .HasForeignKey(f => f.MappingTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MappingFieldData>()
                .HasOne(mf => mf.SourceField)
                .WithMany()
                .HasForeignKey(mf => mf.SourceFieldId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MappingFieldData>()
                .HasOne(mf => mf.TargetField)
                .WithMany()
                .HasForeignKey(mf => mf.TargetFieldId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MappingFieldData>()
                .HasIndex(x => new { x.MappingTemplateId, x.SourceFieldId, x.TargetFieldId })
                .IsUnique();

            modelBuilder.Entity<FileConversionHistoryData>()
                .HasOne(h => h.MappingTemplate)
                .WithMany(m => m.FileConversions)
                .HasForeignKey(h => h.MappingTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UrlConversionHistoryData>()
                .HasOne(h => h.MappingTemplate)
                .WithMany(m => m.UrlConversions)
                .HasForeignKey(h => h.MappingTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UrlFileGettingData>()
                .HasIndex(x => new { x.Url, x.MappingTemplateId })
                .IsUnique();
        }


    }
}
