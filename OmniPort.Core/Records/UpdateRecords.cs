using OmniPort.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
