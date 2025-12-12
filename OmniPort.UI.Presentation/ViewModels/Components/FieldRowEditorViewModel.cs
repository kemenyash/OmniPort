using OmniPort.Core.Enums;
using OmniPort.UI.Presentation.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.ViewModels.Components
{
    public class FieldRowEditorViewModel
    {
        public TemplateFieldRow Field { get; private set; }
        public int Level { get; private set; }

        public void Bind(TemplateFieldRow field, int level)
        {
            Field = field;
            Level = level;
        }

        public void OnTypeChanged()
        {
            if (Field.Type != FieldDataType.Array)
            {
                Field.ItemType = null;
                Field.ChildrenItems.Clear();
            }

            if (Field.Type != FieldDataType.Object)
            {
                Field.Children.Clear();
            }
        }

        public void OnItemTypeChanged()
        {
            if (Field.ItemType != FieldDataType.Object)
            {
                Field.ChildrenItems.Clear();
            }
        }

        public void AddObjectChild()
        {
            Field.Children.Add(new TemplateFieldRow
            {
                Name = "",
                Type = FieldDataType.String
            });
        }

        public void RemoveObjectChild(TemplateFieldRow row)
        {
            Field.Children.Remove(row);
        }

        public void AddArrayItemChild()
        {
            Field.ChildrenItems.Add(new TemplateFieldRow
            {
                Name = "",
                Type = FieldDataType.String
            });
        }

        public void RemoveArrayItemChild(TemplateFieldRow row)
        {
            Field.ChildrenItems.Remove(row);
        }
    }
}
