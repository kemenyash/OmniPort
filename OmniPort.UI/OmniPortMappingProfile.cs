using OmniPort.Core.Models;
using OmniPort.Data;
using AutoMapper;

namespace OmniPort.UI
{
    public class OmniPortMappingProfile : Profile
    {
        public OmniPortMappingProfile()
        {
            // TemplateData -> ImportTemplate
            CreateMap<TemplateData, ImportTemplate>()
                .ForMember(dest => dest.TemplateName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Columns, opt => opt.MapFrom(src => src.Fields.Select(f => f.Name).ToList()))
                .ForMember(dest => dest.SourceType, opt => opt.MapFrom(src => src.SourceType)); // explicitly map enum

            // ImportTemplate -> TemplateData
            CreateMap<ImportTemplate, TemplateData>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.TemplateName))
                .ForMember(dest => dest.Fields, opt => opt.Ignore()) // created manually
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) // set manually
                .ForMember(dest => dest.SourceType, opt => opt.MapFrom(src => src.SourceType)) // restored
                .ForMember(dest => dest.SourceMappings, opt => opt.Ignore())
                .ForMember(dest => dest.TargetMappings, opt => opt.Ignore());

            // TemplateFieldData -> string (just Name)
            CreateMap<TemplateFieldData, string>().ConvertUsing(f => f.Name);

            // TemplateMappingFieldData -> FieldMapping
            CreateMap<TemplateMappingFieldData, FieldMapping>()
                .ForMember(dest => dest.SourceField, opt => opt.MapFrom(src => src.SourceField != null ? src.SourceField.Name : string.Empty))
                .ForMember(dest => dest.TargetField, opt => opt.MapFrom(src => src.TargetField.Name))
                .ForMember(dest => dest.TargetType, opt => opt.MapFrom(src => src.TargetField.Type))
                .ForMember(dest => dest.DateFormat, opt => opt.Ignore())
                .ForMember(dest => dest.CustomTransform, opt => opt.Ignore());

            // TemplateMappingData -> ImportProfile
            CreateMap<TemplateMappingData, ImportProfile>()
                .ForMember(dest => dest.ProfileName, opt => opt.MapFrom(src => $"Map {src.SourceTemplate.Name} → {src.TargetTemplate.Name}"))
                .ForMember(dest => dest.Template, opt => opt.MapFrom(src => src.TargetTemplate))
                .ForMember(dest => dest.Mappings, opt => opt.Ignore()); // mapped elsewhere
        }
    }
}
