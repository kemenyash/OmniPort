using Microsoft.AspNetCore.Components;
using OmniPort.UI.Presentation.ViewModels.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.Inherits.Pages
{
    public class JoinTemplatesBase : ComponentBase, IDisposable
    {
        [Inject]
        protected JoinTemplatesViewModel ViewModel { get; set; }

        protected override async Task OnInitializedAsync()
        {
            ViewModel.Changed += OnViewModelChanged;
            await ViewModel.InitializeAsync();
        }

        public void Dispose()
        {
            ViewModel.Changed -= OnViewModelChanged;
        }

        private void OnViewModelChanged()
        {
            _ = InvokeAsync(StateHasChanged);
        }

        protected async Task OnSourceChanged(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int id))
            {
                await ViewModel.SetSourceTemplate(id);
                StateHasChanged();
            }
        }

        protected async Task OnTargetChanged(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int id))
            {
                await ViewModel.SetTargetTemplate(id);
                StateHasChanged();
            }
        }

        protected void OnMapFieldChanged(string targetPath, ChangeEventArgs e)
        {
            ViewModel.MapField(targetPath, e.Value?.ToString());
            StateHasChanged();
        }

        protected async Task SaveMapping()
        {
            await ViewModel.SaveMapping();
            StateHasChanged();
        }

        protected async Task DeleteJoinTemplate(int id)
        {
            await ViewModel.DeleteJoinTemplate(id);
            StateHasChanged();
        }
    }
}
