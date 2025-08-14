namespace OmniPort.Core.Records
{
    using OmniPort.Core.Models;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public record CreateBasicTemplateDto(
        [property: Required] string Name,
        [property: Required] SourceType SourceType,
        [property: MinLength(1)] IReadOnlyList<CreateTemplateFieldDto> Fields
    );

    public record CreateTemplateFieldDto(
        [property: Required] string Name,
        [property: Required] FieldDataType Type
    );

    public record UpdateBasicTemplateDto(
        [property: Required] int Id,
        [property: Required] string Name,
        [property: Required] SourceType SourceType,
        [property: MinLength(1)] IReadOnlyList<UpsertTemplateFieldDto> Fields
    );

    public record UpsertTemplateFieldDto(
        int? Id, 
        [property: Required] string Name,
        [property: Required] FieldDataType Type
    );

    public record CreateMappingTemplateDto(
        [property: Required] string Name,
        [property: Required] int SourceTemplateId,
        [property: Required] int TargetTemplateId,
        [property: Required] IReadOnlyDictionary<int, int?> TargetToSource
    );

    public record UpdateMappingTemplateDto(
        [property: Required] int Id,
        [property: Required] string Name,
        [property: Required] int SourceTemplateId,
        [property: Required] int TargetTemplateId,
        [property: Required] IReadOnlyDictionary<int, int?> TargetToSource
    );

    public record AddWatchedUrlDto(
        [property: Required, Url] string Url,
        [property: Range(1, 24 * 60)] int IntervalMinutes
    );
}
