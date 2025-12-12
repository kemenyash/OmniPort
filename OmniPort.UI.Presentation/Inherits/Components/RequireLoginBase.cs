using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.Inherits.Components
{
    public class RequireLoginBase : ComponentBase
    {
        [Inject]
        protected NavigationManager Nav { get; set; }

        protected override void OnInitialized()
        {
            var uri = new Uri(Nav.Uri);

            if (!uri.AbsolutePath.Equals("/login", StringComparison.OrdinalIgnoreCase))
            {
                var returnUrl = Uri.EscapeDataString(uri.PathAndQuery);
                Nav.NavigateTo($"/login?returnUrl={returnUrl}", forceLoad: true);
            }
        }
    }
}
