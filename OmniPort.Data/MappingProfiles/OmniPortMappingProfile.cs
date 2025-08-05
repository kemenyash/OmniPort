using AutoMapper;
using OmniPort.Core.Models;
using OmniPort.Data;

namespace OmniPort.Data.MappingProfiles
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
                .ForMember(dest => dest.TargetTemplate, opt => opt.MapFrom(src => src.TargetTemplate.Name));

        }
    }
}
