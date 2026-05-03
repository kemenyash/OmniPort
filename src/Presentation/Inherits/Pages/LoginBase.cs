using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Presentation.ViewModels.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Presentation.Inherits.Pages
{
    public class LoginBase : ComponentBase
    {
        [CascadingParameter]
        protected HttpContext? Context { get; set; }

        [Inject]
        protected LoginViewModel ViewModel { get; set; } = null!;

        protected override void OnParametersSet()
        {
            bool hasError = Context?.Request?.Query.ContainsKey("e") == true;
            ViewModel.SetInvalidCredentials(hasError);
        }
    }
}
