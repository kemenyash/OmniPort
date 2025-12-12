using Microsoft.AspNetCore.Components;
using OmniPort.UI.Presentation.ViewModels.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.Inherits.Pages
{
    public class IndexBase : ComponentBase
    {
        [Inject]
        protected IndexViewModel ViewModel { get; set; }

        protected void OpenTemplates()
        {
            ViewModel.OpenTemplates();
            StateHasChanged();
        }

        protected void CloseTemplates()
        {
            ViewModel.CloseTemplates();
            StateHasChanged();
        }

        protected void OpenJoinTemplates()
        {
            ViewModel.OpenJoinTemplates();
            StateHasChanged();
        }

        protected void CloseJoinTemplates()
        {
            ViewModel.CloseJoinTemplates();
            StateHasChanged();
        }
    }
}
