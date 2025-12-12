using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OmniPort.UI.Presentation.Models;
using OmniPort.UI.Presentation.ViewModels.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.Inherits.Pages
{
    public class TransformationBase : ComponentBase, IDisposable
    {
        [Inject]
        protected TransformationViewModel ViewModel { get; set; }

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

        protected void SetMode(UploadMode mode)
        {
            ViewModel.SetMode(mode);
            StateHasChanged();
        }

        protected Task OnFileChange(InputFileChangeEventArgs e)
        {
            ViewModel.SetUploadedFile(e.File);
            StateHasChanged();
            return Task.CompletedTask;
        }

        protected async Task RunTransformation(EditContext editContext)
        {
            await ViewModel.RunTransformation();
            StateHasChanged();
        }

        protected async Task AddToWatchlistFromForm()
        {
            await ViewModel.AddToWatchlistFromForm();
            StateHasChanged();
        }
    }
}
