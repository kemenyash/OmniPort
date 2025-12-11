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
        public static IReadOnlyList<(string Path, FieldDataType Type)> Flatten(TemplateFieldDto templateField, string prefix = "")
        {
            var list = new List<(string, FieldDataType)>();
            var name = string.IsNullOrEmpty(prefix) ? templateField.Name : $"{prefix}.{templateField.Name}";

            switch (templateField.Type)
            {
                case FieldDataType.Object:
                    if (templateField.Children?.Count > 0)
                    {
                        foreach (var child in templateField.Children)
                        {
                            list.AddRange(Flatten(child, name));
                        }
                    }
                    else
                    {
                        list.Add((name, FieldDataType.Object));
                    }
                    break;

                case FieldDataType.Array:
                    var arrBase = $"{name}[]";
                    if (templateField.ItemType == FieldDataType.Object)
                    {
                        if (templateField.ChildrenItems?.Count > 0)
                        {
                            foreach (var child in templateField.ChildrenItems)
                            {
                                list.AddRange(Flatten(child, arrBase));
                            }
                        }
                        else
                        {
                            list.Add((arrBase, FieldDataType.Object));
                        }
                    }
                    else
                    {
                        list.Add((arrBase, templateField.ItemType ?? FieldDataType.String));
                    }
                    break;

                default:
                    list.Add((name, templateField.Type));
                    break;
            }
            return list;
        }

        public static IReadOnlyList<(string Path, FieldDataType Type)> FlattenMany(IEnumerable<TemplateFieldDto> fields)
        {
            var result = new List<(string, FieldDataType)>();
            foreach (var field in fields)
            {
                result.AddRange(Flatten(field));
            }
            return result;
        }
    }

}
