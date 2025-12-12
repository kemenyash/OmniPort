using Microsoft.AspNetCore.Components;
using OmniPort.UI.Presentation.Models;
using OmniPort.UI.Presentation.ViewModels.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.Inherits.Pages
{
    public class TemplatesBase : ComponentBase, IDisposable
    {
        [Inject]
        protected TemplateEditorViewModel ViewModel { get; set; }

        protected override async Task OnInitializedAsync()
        {
            ViewModel.Changed += OnViewModelChanged;
            await ViewModel.Initialize();
        }

        public void Dispose()
        {
            ViewModel.Changed -= OnViewModelChanged;
        }

        private void OnViewModelChanged()
        {
            _ = InvokeAsync(StateHasChanged);
        }

        protected void StartCreate()
        {
            ViewModel.StartCreate();
            StateHasChanged();
        }

        protected async Task StartEdit(int id)
        {
            await ViewModel.StartEdit(id);
            StateHasChanged();
        }

        protected void CancelEdit()
        {
            ViewModel.CancelEdit();
            StateHasChanged();
        }

        protected void AddField()
        {
            ViewModel.AddField();
            StateHasChanged();
        }

        protected void RemoveField(TemplateFieldRow row)
        {
            ViewModel.RemoveField(row);
            StateHasChanged();
        }

        protected async Task Save()
        {
            await ViewModel.Save();
            StateHasChanged();
        }

        protected async Task Delete(int id)
        {
            await ViewModel.Delete(id);
            StateHasChanged();
        }
    }
}
