namespace OmniPort.Core.Records
{
    using OmniPort.Core.Enums;
    using System;
    using System.Collections.Generic;

    public record TemplateFieldDto(
        int Id,
        string Name,
        FieldDataType Type
    );

    public record BasicTemplateDto(
        int Id,
        string Name,
        SourceType SourceType,
        IReadOnlyList<TemplateFieldDto> Fields
    );

    public record TemplateSummaryDto(
        int Id,
        string Name,
        SourceType SourceType,
        int FieldsCount
    );

    public record MappingFieldDto(
        int Id,
        int SourceFieldId,
        string SourceFieldName,
        FieldDataType SourceFieldType,
        int TargetFieldId,
        string TargetFieldName,
        FieldDataType TargetFieldType
    );

    public record MappingTemplateDto(
        int Id,
        string Name,
        int SourceTemplateId,
        string SourceTemplateName,
        int TargetTemplateId,
        string TargetTemplateName,
        IReadOnlyList<MappingFieldDto> Fields
    );

    public record JoinedTemplateSummaryDto(
        int Id,
        string SourceTemplate,
        string TargetTemplate,
        SourceType OutputFormat 
    )
    {
        public string FullName => $"{SourceTemplate} → {TargetTemplate}";
    }

    public record FileConversionHistoryDto(
        int Id,
        DateTime ConvertedAt,
        string FileName,
        string OutputLink,
        int MappingTemplateId,
        string MappingTemplateName
    );

    public record UrlConversionHistoryDto(
        int Id,
        DateTime ConvertedAt,
        string InputUrl,
        string OutputLink,
        int MappingTemplateId,
        string MappingTemplateName
    );

    public record WatchedUrlDto(
        int Id,
        string Url,
        int IntervalMinutes
    );
}
