namespace OmniPort.Core.Records
{
    using OmniPort.Core.Enums;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public record CreateBasicTemplateDto(
        string Name,
        SourceType SourceType,
        IReadOnlyList<CreateTemplateFieldDto> Fields
    );

    public record CreateTemplateFieldDto(
        string Name,
        FieldDataType Type,
        FieldDataType? ItemType,
        IReadOnlyList<CreateTemplateFieldDto> Children,
        IReadOnlyList<CreateTemplateFieldDto> ChildrenItems
    );


    public record CreateMappingTemplateDto(
        string Name,
        int SourceTemplateId,
        int TargetTemplateId,
        IReadOnlyList<MappingEntryDto> Mappings
    );

    public record AddWatchedUrlDto(
        [property: Required, Url] string Url,
        [property: Range(1, 24 * 60)] int IntervalMinutes,
        [property: Required] int MappingTemplateId
    );
}
