using Microsoft.AspNetCore.Components;
using OmniPort.UI.Presentation.Models;
using OmniPort.UI.Presentation.ViewModels.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.Inherits.Components
{
    public class FieldRowEditorBase : ComponentBase
    {
        [Parameter] public TemplateFieldRow Field { get; set; }
        [Parameter] public int Level { get; set; }
        [Parameter] public Action? OnRemove { get; set; }

        protected FieldRowEditorViewModel ViewModel { get; } = new();

        protected override void OnParametersSet()
        {
            ViewModel.Bind(Field, Level);
        }

        protected void OnTypeChanged()
        {
            ViewModel.OnTypeChanged();
            StateHasChanged();
        }

        protected void OnItemTypeChanged()
        {
            ViewModel.OnItemTypeChanged();
            StateHasChanged();
        }

        protected void AddObjectChild()
        {
            ViewModel.AddObjectChild();
            StateHasChanged();
        }

        protected void RemoveObjectChild(TemplateFieldRow row)
        {
            ViewModel.RemoveObjectChild(row);
            StateHasChanged();
        }

        protected void AddArrayItemChild()
        {
            ViewModel.AddArrayItemChild();
            StateHasChanged();
        }

        protected void RemoveArrayItemChild(TemplateFieldRow row)
        {
            ViewModel.RemoveArrayItemChild(row);
            StateHasChanged();
        }

        protected void RemoveSelf()
        {
            OnRemove?.Invoke();
        }
    }
}
