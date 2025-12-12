using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using OmniPort.UI.Presentation.ViewModels.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.Inherits.Components
{
    public class ErrorBase : ComponentBase
    {
        [Inject]
        protected ErrorViewModel ViewModel { get; set; }

        [CascadingParameter]
        protected HttpContext? HttpContext { get; set; }

        protected override void OnInitialized()
        {
            ViewModel.Initialize(HttpContext);
        }
    }
}
