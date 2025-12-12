using OmniPort.Core.Enums;
using OmniPort.Core.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.ViewModels.Components
{
    public class SourceFieldTreeViewModel
    {
        public TemplateFieldDto Node { get; private set; } = default!;

        public string Name => Node.Name;
        public string TypeText => BuildTypeText(Node);

        public IReadOnlyList<TemplateFieldDto> Children { get; private set; } = Array.Empty<TemplateFieldDto>();
        public IReadOnlyList<TemplateFieldDto> ItemChildren { get; private set; } = Array.Empty<TemplateFieldDto>();

        public bool HasChildren => Children.Count > 0 || ItemChildren.Count > 0;

        public void Bind(TemplateFieldDto node)
        {
            Node = node;

            Children = (node.Children ?? Enumerable.Empty<TemplateFieldDto>()).ToList();
            ItemChildren = (node.ChildrenItems ?? Enumerable.Empty<TemplateFieldDto>()).ToList();
        }

        private static string BuildTypeText(TemplateFieldDto node)
        {
            string typeText = node.Type.ToString();

            if (node.Type == FieldDataType.Array && node.ItemType.HasValue)
            {
                typeText += $"[{node.ItemType}]";
            }

            return typeText;
        }
    }
}
