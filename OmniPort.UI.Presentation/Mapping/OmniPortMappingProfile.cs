using AutoMapper;
using OmniPort.Core.Models;
using OmniPort.Core.Records;
using OmniPort.Data;
using OmniPort.UI.Presentation.Models;
using System.Linq;

namespace OmniPort.UI.Presentation.Mapping
{
    public class OmniPortMappingProfile : Profile
    {
        public OmniPortMappingProfile()
        {
            CreateMap<FieldData, TemplateFieldDto>()
                .ForCtorParam("Children", opt => opt.MapFrom(s => s.Children.Where(c => !c.IsArrayItem)))
                .ForCtorParam("ChildrenItems", opt => opt.MapFrom(s => s.Children.Where(c => c.IsArrayItem)))
                .ForCtorParam("ItemType", opt => opt.MapFrom(s => s.ItemType));

            CreateMap<BasicTemplateData, BasicTemplateDto>()
                .ForCtorParam("Fields", opt => opt.MapFrom(s => s.Fields.Where(f => f.ParentFieldId == null && !f.IsArrayItem)));

            CreateMap<BasicTemplateData, TemplateSummaryDto>()
                .ForCtorParam("FieldsCount", opt => opt.MapFrom(s =>
                    s.Fields.Count(f => f.ParentFieldId == null && !f.IsArrayItem)));

            CreateMap<MappingFieldData, MappingFieldDto>()
                .ForCtorParam("SourceFieldName", opt => opt.MapFrom(s => s.SourceField.Name))
                .ForCtorParam("SourceFieldType", opt => opt.MapFrom(s => s.SourceField.Type))
                .ForCtorParam("TargetFieldName", opt => opt.MapFrom(s => s.TargetField.Name))
                .ForCtorParam("TargetFieldType", opt => opt.MapFrom(s => s.TargetField.Type));

            CreateMap<MappingTemplateData, MappingTemplateDto>()
                .ForCtorParam("SourceTemplateName", opt => opt.MapFrom(s => s.SourceTemplate.Name))
                .ForCtorParam("TargetTemplateName", opt => opt.MapFrom(s => s.TargetTemplate.Name))
                .ForCtorParam("Fields", opt => opt.MapFrom(s => s.MappingFields));

            CreateMap<MappingTemplateData, JoinedTemplateSummaryDto>()
                .ForCtorParam("SourceTemplate", opt => opt.MapFrom(s => s.SourceTemplate.Name))
                .ForCtorParam("TargetTemplate", opt => opt.MapFrom(s => s.TargetTemplate.Name))
                .ForCtorParam("OutputFormat", opt => opt.MapFrom(s => s.TargetTemplate.SourceType));

            CreateMap<FileConversionHistoryData, FileConversionHistoryDto>()
                .ForCtorParam("OutputLink", opt => opt.MapFrom(s => s.OutputUrl))
                .ForCtorParam("MappingTemplateName", opt => opt.MapFrom(s => s.MappingTemplate.Name));

            CreateMap<UrlConversionHistoryData, UrlConversionHistoryDto>()
                .ForCtorParam("OutputLink", opt => opt.MapFrom(s => s.OutputUrl))
                .ForCtorParam("InputUrl", opt => opt.MapFrom(s => s.InputUrl))
                .ForCtorParam("MappingTemplateName", opt => opt.MapFrom(s => s.MappingTemplate.Name));

            CreateMap<UrlFileGettingData, WatchedUrlDto>()
                .ForCtorParam("MappingTemplateId", opt => opt.MapFrom(s => s.MappingTemplateId))
                .ForCtorParam("MappingTemplateName", opt => opt.MapFrom(s => s.MappingTemplate.Name))
                .ForCtorParam("IntervalMinutes", opt => opt.MapFrom(s => s.CheckIntervalMinutes));


            CreateMap<CreateBasicTemplateDto, BasicTemplateData>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.Fields, opt => opt.Ignore())
                .ForMember(d => d.AsSourceMappings, opt => opt.Ignore())
                .ForMember(d => d.AsTargetMappings, opt => opt.Ignore());

            CreateMap<UpdateBasicTemplateDto, BasicTemplateData>()
                .ForMember(d => d.Fields, opt => opt.Ignore())
                .ForMember(d => d.AsSourceMappings, opt => opt.Ignore())
                .ForMember(d => d.AsTargetMappings, opt => opt.Ignore());

            CreateMap<CreateTemplateFieldDto, FieldData>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.TemplateSourceId, opt => opt.Ignore())
                .ForMember(d => d.TemplateSource, opt => opt.Ignore())
                .ForMember(d => d.ParentFieldId, opt => opt.Ignore())
                .ForMember(d => d.IsArrayItem, opt => opt.Ignore())
                .ForMember(d => d.Children, opt => opt.Ignore());

            CreateMap<UpsertTemplateFieldDto, FieldData>()
                .ForMember(d => d.TemplateSourceId, opt => opt.Ignore())
                .ForMember(d => d.TemplateSource, opt => opt.Ignore())
                .ForMember(d => d.ParentFieldId, opt => opt.Ignore())
                .ForMember(d => d.IsArrayItem, opt => opt.Ignore())
                .ForMember(d => d.Children, opt => opt.Ignore());

            CreateMap<CreateMappingTemplateDto, MappingTemplateData>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.SourceTemplate, opt => opt.Ignore())
                .ForMember(d => d.TargetTemplate, opt => opt.Ignore())
                .ForMember(d => d.FileConversions, opt => opt.Ignore())
                .ForMember(d => d.UrlConversions, opt => opt.Ignore())
                .ForMember(d => d.MappingFields, opt => opt.Ignore());

            CreateMap<UpdateMappingTemplateDto, MappingTemplateData>()
                .ForMember(d => d.SourceTemplate, opt => opt.Ignore())
                .ForMember(d => d.TargetTemplate, opt => opt.Ignore())
                .ForMember(d => d.FileConversions, opt => opt.Ignore())
                .ForMember(d => d.UrlConversions, opt => opt.Ignore())
                .ForMember(d => d.MappingFields, opt => opt.Ignore());

            CreateMap<TemplateEditForm, BasicTemplateData>()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id ?? 0))
                .ForMember(d => d.Fields, opt => opt.Ignore())
                .ForMember(d => d.AsSourceMappings, opt => opt.Ignore())
                .ForMember(d => d.AsTargetMappings, opt => opt.Ignore());

            CreateMap<TemplateFieldRow, FieldData>()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id ?? 0))
                .ForMember(d => d.TemplateSourceId, opt => opt.Ignore())
                .ForMember(d => d.TemplateSource, opt => opt.Ignore())
                .ForMember(d => d.ParentFieldId, opt => opt.Ignore())
                .ForMember(d => d.IsArrayItem, opt => opt.Ignore())
                .ForMember(d => d.Children, opt => opt.Ignore());

            CreateMap<MappingTemplateForm, MappingTemplateData>()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id ?? 0))
                .ForMember(d => d.SourceTemplate, opt => opt.Ignore())
                .ForMember(d => d.TargetTemplate, opt => opt.Ignore())
                .ForMember(d => d.FileConversions, opt => opt.Ignore())
                .ForMember(d => d.UrlConversions, opt => opt.Ignore())
                .ForMember(d => d.MappingFields, opt => opt.Ignore());
        }
    }
}
