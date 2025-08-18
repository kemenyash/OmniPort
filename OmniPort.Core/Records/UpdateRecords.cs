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

    public record UpdateMappingTemplateDto(
    [property: Required] int Id,
    [property: Required] string Name,
    [property: Required] int SourceTemplateId,
    [property: Required] int TargetTemplateId,
    [property: Required] IReadOnlyDictionary<int, int?> TargetToSource
);
}
