using OmniPort.Core.Enums;

namespace OmniPort.Core.Records
{
    public record UpdateBasicTemplateDto(
       int Id,
       string Name,
       SourceType SourceType,
       IReadOnlyList<UpsertTemplateFieldDto> Fields
    );

    public record UpsertTemplateFieldDto(
        int? Id,
        string Name,
        FieldDataType Type,
        FieldDataType? ItemType,
        IReadOnlyList<UpsertTemplateFieldDto> Children,
        IReadOnlyList<UpsertTemplateFieldDto> ChildrenItems
    );

    public record UpdateMappingTemplateDto(
        int Id,
        string Name,
        int SourceTemplateId,
        int TargetTemplateId,
        IReadOnlyList<MappingEntryDto> Mappings
    );
}
