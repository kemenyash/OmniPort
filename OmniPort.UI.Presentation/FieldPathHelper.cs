using OmniPort.Core.Enums;
using OmniPort.Core.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation
{
    public static class FieldPathHelper
    {
        public static IReadOnlyList<(string Path, FieldDataType Type)> Flatten(TemplateFieldDto f, string prefix = "")
        {
            var list = new List<(string, FieldDataType)>();
            var name = string.IsNullOrEmpty(prefix) ? f.Name : $"{prefix}.{f.Name}";

            switch (f.Type)
            {
                case FieldDataType.Object:
                    if (f.Children?.Count > 0)
                        foreach (var c in f.Children) list.AddRange(Flatten(c, name));
                    else
                        list.Add((name, FieldDataType.Object));
                    break;

                case FieldDataType.Array:
                    var arrBase = $"{name}[]";
                    if (f.ItemType == FieldDataType.Object)
                    {
                        if (f.ChildrenItems?.Count > 0)
                            foreach (var c in f.ChildrenItems) list.AddRange(Flatten(c, arrBase));
                        else
                            list.Add((arrBase, FieldDataType.Object));
                    }
                    else
                    {
                        list.Add((arrBase, f.ItemType ?? FieldDataType.String));
                    }
                    break;

                default:
                    list.Add((name, f.Type));
                    break;
            }
            return list;
        }

        public static IReadOnlyList<(string Path, FieldDataType Type)> FlattenMany(IEnumerable<TemplateFieldDto> fields)
        {
            var result = new List<(string, FieldDataType)>();
            foreach (var f in fields) result.AddRange(Flatten(f));
            return result;
        }
    }

}
