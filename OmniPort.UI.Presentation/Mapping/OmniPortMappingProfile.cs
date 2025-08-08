using AutoMapper;
using OmniPort.Core.Models;
using OmniPort.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.Mapping
{
    public class OmniPortMappingProfile : Profile
    {
        public OmniPortMappingProfile()
        {
            // Template <-> ImportTemplate
            CreateMap<TemplateData, ImportTemplate>()
                .ForMember(dest => dest.TemplateName, opt => opt.MapFrom(src => src.Name))
                .ReverseMap()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.TemplateName));

            // TemplateFieldData <-> TemplateField 
            CreateMap<TemplateFieldData, TemplateField>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
                .ReverseMap();

            // TemplateFieldData <-> FieldMapping 
            CreateMap<TemplateFieldData, FieldMapping>()
                .ForMember(dest => dest.SourceField, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.TargetType, opt => opt.MapFrom(src => src.Type))
                .ReverseMap()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.SourceField))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.TargetType))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TemplateId, opt => opt.Ignore());

            // TemplateSummary proection
            CreateMap<TemplateData, TemplateSummary>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name));

            // TemplateMappingData -> ImportProfile
            CreateMap<TemplateMappingData, ImportProfile>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Template, opt => opt.MapFrom(src => src.TargetTemplate))
                .ForMember(dest => dest.Mappings, opt => opt.Ignore());

            // TemplateMappingFieldData -> FieldMapping
            CreateMap<TemplateMappingFieldData, FieldMapping>()
                .ForMember(dest => dest.SourceField, opt => opt.MapFrom(src => src.SourceField != null ? src.SourceField.Name : null))
                .ForMember(dest => dest.TargetField, opt => opt.MapFrom(src => src.TargetField.Name))
                .ForMember(dest => dest.TargetType, opt => opt.MapFrom(src => src.TargetField.Type));

            // Dictionary<int, int?> -> List<TemplateMappingFieldData>
            CreateMap<KeyValuePair<int, int?>, TemplateMappingFieldData>()
                .ForMember(dest => dest.TargetFieldId, opt => opt.MapFrom(src => src.Key))
                .ForMember(dest => dest.SourceFieldId, opt => opt.MapFrom(src => src.Value))
                .ForMember(dest => dest.MappingId, opt => opt.Ignore())
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TargetField, opt => opt.Ignore())
                .ForMember(dest => dest.SourceField, opt => opt.Ignore());


            CreateMap<TemplateMappingData, JoinedTemplateSummary>()
                .ForMember(dest => dest.SourceTemplate, opt => opt.MapFrom(src => src.SourceTemplate.Name))
                .ForMember(dest => dest.OutputFormat, opt => opt.MapFrom(src => src.TargetTemplate.SourceType))
                .ForMember(dest => dest.TargetTemplate, opt => opt.MapFrom(src => src.TargetTemplate.Name));


            // FileConversionData <-> ConversionHistory
            CreateMap<FileConversionData, ConversionHistory>()
                .ForMember(dest => dest.TemplateMapId, opt => opt.MapFrom(src => src.TemplateMapId))
                .ForMember(dest => dest.FileName, opt => opt.MapFrom(src => src.FileName))
                .ForMember(dest => dest.ConvertedAt, opt => opt.MapFrom(src => src.ConvertedAt))
                .ForMember(dest => dest.OutputLink, opt => opt.MapFrom(src => src.OutputUrl))
                .ForMember(dest => dest.TemplateName, opt => opt.MapFrom(src =>
                    src.TemplateMap != null && src.TemplateMap.SourceField != null && src.TemplateMap.TargetField != null
                        ? $"{src.TemplateMap.SourceField.Name} → {src.TemplateMap.TargetField.Name}"
                        : "Unknown Template"))
                .ReverseMap()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.OutputUrl, opt => opt.MapFrom(src => src.OutputLink))
                .ForMember(dest => dest.ConvertedAt, opt => opt.MapFrom(src => src.ConvertedAt))
                .ForMember(dest => dest.FileName, opt => opt.MapFrom(src => src.FileName));

            // UrlConversionData <-> UrlConversionHistory
            CreateMap<UrlConversionData, UrlConversionHistory>()
                .ForMember(dest => dest.TemplateMapId, opt => opt.MapFrom(src => src.TemplateMapId))
                .ForMember(dest => dest.InputUrl, opt => opt.MapFrom(src => src.InputUrl))
                .ForMember(dest => dest.ConvertedAt, opt => opt.MapFrom(src => src.ConvertedAt))
                .ForMember(dest => dest.OutputLink, opt => opt.MapFrom(src => src.OutputUrl))
                 .ForMember(dest => dest.TemplateName, opt => opt.MapFrom(src =>
                    src.TemplateMap != null && src.TemplateMap.SourceField != null && src.TemplateMap.TargetField != null
                        ? $"{src.TemplateMap.SourceField.Name} → {src.TemplateMap.TargetField.Name}"
                        : "Unknown Template"))
                .ReverseMap()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.OutputUrl, opt => opt.MapFrom(src => src.OutputLink))
                .ForMember(dest => dest.ConvertedAt, opt => opt.MapFrom(src => src.ConvertedAt))
                .ForMember(dest => dest.InputUrl, opt => opt.MapFrom(src => src.InputUrl));

            // WatchedUrlData <-> WatchedUrl
            CreateMap<WatchedUrlData, WatchedUrl>()
                .ForMember(dest => dest.Url, opt => opt.MapFrom(src => src.Url))
                .ForMember(dest => dest.IntervalMinutes, opt => opt.MapFrom(src => src.IntervalMinutes))
                .ReverseMap()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Url, opt => opt.MapFrom(src => src.Url))
                .ForMember(dest => dest.IntervalMinutes, opt => opt.MapFrom(src => src.IntervalMinutes))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());


        }
    }
}
