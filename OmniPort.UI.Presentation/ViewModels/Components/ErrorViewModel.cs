using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.ViewModels.Components
{
    public class ErrorViewModel
    {
        public string? RequestId { get; private set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public void Initialize(HttpContext? httpContext)
        {
            RequestId = Activity.Current?.Id ?? httpContext?.TraceIdentifier;
        }
    }
}
