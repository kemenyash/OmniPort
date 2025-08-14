using AutoMapper;
using OmniPort.Core.Models;
using OmniPort.Core.Records;
using OmniPort.Data;
using System.Collections.Generic;
using System.Linq;

namespace OmniPort.UI.Presentation.Mapping
{
    public class OmniPortMappingProfile : Profile
    {
        public OmniPortMappingProfile()
        {
            // ----------- Entity -> DTO -----------

            // Field
            CreateMap<FieldData, TemplateFieldDto>();

            // Basic Template
            CreateMap<BasicTemplateData, BasicTemplateDto>()
                .ForCtorParam("Fields", opt => opt.MapFrom(s => s.Fields));

            CreateMap<BasicTemplateData, TemplateSummaryDto>()
                .ForCtorParam("FieldsCount", opt => opt.MapFrom(s => s.Fields.Count));

            // Mapping Field (enriched with names/types)
            CreateMap<MappingFieldData, MappingFieldDto>()
                .ForCtorParam("SourceFieldName", opt => opt.MapFrom(s => s.SourceField.Name))
                .ForCtorParam("SourceFieldType", opt => opt.MapFrom(s => s.SourceField.Type))
                .ForCtorParam("TargetFieldName", opt => opt.MapFrom(s => s.TargetField.Name))
                .ForCtorParam("TargetFieldType", opt => opt.MapFrom(s => s.TargetField.Type));

            // Mapping Template
            CreateMap<MappingTemplateData, MappingTemplateDto>()
                .ForCtorParam("SourceTemplateName", opt => opt.MapFrom(s => s.SourceTemplate.Name))
                .ForCtorParam("TargetTemplateName", opt => opt.MapFrom(s => s.TargetTemplate.Name))
                .ForCtorParam("Fields", opt => opt.MapFrom(s => s.MappingFields));

            // Joined summary (for selectors)
            CreateMap<MappingTemplateData, JoinedTemplateSummaryDto>()
                .ForCtorParam("SourceTemplate", opt => opt.MapFrom(s => s.SourceTemplate.Name))
                .ForCtorParam("TargetTemplate", opt => opt.MapFrom(s => s.TargetTemplate.Name))
                .ForCtorParam("OutputFormat", opt => opt.MapFrom(s => s.TargetTemplate.SourceType));

            // History
            CreateMap<FileConversionHistoryData, FileConversionHistoryDto>()
                .ForCtorParam("OutputLink", opt => opt.MapFrom(s => s.OutputUrl))
                .ForCtorParam("MappingTemplateName", opt => opt.MapFrom(s => s.MappingTemplate.Name));

            CreateMap<UrlConversionHistoryData, UrlConversionHistoryDto>()
                .ForCtorParam("OutputLink", opt => opt.MapFrom(s => s.OutputUrl))
                .ForCtorParam("InputUrl", opt => opt.MapFrom(s => s.InputUrl))
                .ForCtorParam("MappingTemplateName", opt => opt.MapFrom(s => s.MappingTemplate.Name));

            CreateMap<UrlFileGettingData, WatchedUrlDto>()
                .ForCtorParam("IntervalMinutes", opt => opt.MapFrom(s => s.CheckIntervalMinutes));


            // ----------- DTO(Create/Update/Form) -> Entity -----------

            // CreateBasicTemplateDto -> BasicTemplateData
            CreateMap<CreateBasicTemplateDto, BasicTemplateData>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.Fields, opt => opt.MapFrom(s => s.Fields))
                .ForMember(d => d.AsSourceMappings, opt => opt.Ignore())
                .ForMember(d => d.AsTargetMappings, opt => opt.Ignore());

            CreateMap<CreateTemplateFieldDto, FieldData>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.TemplateSourceId, opt => opt.Ignore()) 
                .ForMember(d => d.TemplateSource, opt => opt.Ignore());

            // UpdateBasicTemplateDto
            CreateMap<UpsertTemplateFieldDto, FieldData>()
                .ForMember(d => d.TemplateSourceId, opt => opt.Ignore())
                .ForMember(d => d.TemplateSource, opt => opt.Ignore());

            // CreateMappingTemplateDto -> MappingTemplateData
            CreateMap<CreateMappingTemplateDto, MappingTemplateData>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.MappingFields, opt => opt.Ignore()) 
                .ForMember(d => d.SourceTemplate, opt => opt.Ignore())
                .ForMember(d => d.TargetTemplate, opt => opt.Ignore())
                .ForMember(d => d.FileConversions, opt => opt.Ignore())
                .ForMember(d => d.UrlConversions, opt => opt.Ignore());

            // UpdateMappingTemplateDto -> MappingTemplateData
            CreateMap<UpdateMappingTemplateDto, MappingTemplateData>()
                .ForMember(d => d.MappingFields, opt => opt.Ignore())
                .ForMember(d => d.SourceTemplate, opt => opt.Ignore())
                .ForMember(d => d.TargetTemplate, opt => opt.Ignore())
                .ForMember(d => d.FileConversions, opt => opt.Ignore())
                .ForMember(d => d.UrlConversions, opt => opt.Ignore());

            // Forms
            CreateMap<TemplateEditForm, BasicTemplateData>()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id ?? 0))
                .ForMember(d => d.Fields, opt => opt.Ignore()) 
                .ForMember(d => d.AsSourceMappings, opt => opt.Ignore())
                .ForMember(d => d.AsTargetMappings, opt => opt.Ignore());

            CreateMap<TemplateFieldRow, FieldData>()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id ?? 0))
                .ForMember(d => d.TemplateSourceId, opt => opt.Ignore())
                .ForMember(d => d.TemplateSource, opt => opt.Ignore());

            CreateMap<MappingTemplateForm, MappingTemplateData>()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id ?? 0))
                .ForMember(d => d.MappingFields, opt => opt.Ignore())
                .ForMember(d => d.SourceTemplate, opt => opt.Ignore())
                .ForMember(d => d.TargetTemplate, opt => opt.Ignore())
                .ForMember(d => d.FileConversions, opt => opt.Ignore())
                .ForMember(d => d.UrlConversions, opt => opt.Ignore());
        }


        public static void UpsertFields(
            BasicTemplateData entity,
            IEnumerable<UpsertTemplateFieldDto> fieldsDto)
        {
            var byId = entity.Fields.ToDictionary(f => f.Id);
            var incomingIds = new HashSet<int>();

            foreach (var f in fieldsDto)
            {
                if (f.Id.HasValue && byId.TryGetValue(f.Id.Value, out var existing))
                {
                    existing.Name = f.Name;
                    existing.Type = f.Type;
                    incomingIds.Add(existing.Id);
                }
                else
                {
                    entity.Fields.Add(new FieldData
                    {
                        Name = f.Name,
                        Type = f.Type
                    });
                }
            }

            var toRemove = entity.Fields.Where(f => f.Id != 0 && !incomingIds.Contains(f.Id)).ToList();
            foreach (var rem in toRemove) entity.Fields.Remove(rem);
        }
        public static List<MappingFieldData> BuildMappingFields(
            int mappingTemplateId,
            IReadOnlyDictionary<int, int?> targetToSource)
        {
            var result = new List<MappingFieldData>(targetToSource.Count);
            foreach (var kv in targetToSource)
            {
                var targetId = kv.Key;
                var sourceId = kv.Value;

                if (sourceId is null) continue; 

                result.Add(new MappingFieldData
                {
                    MappingTemplateId = mappingTemplateId,
                    TargetFieldId = targetId,
                    SourceFieldId = sourceId.Value
                });
            }
            return result;
        }
    }
}
