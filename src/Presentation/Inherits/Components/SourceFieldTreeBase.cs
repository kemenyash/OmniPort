using Microsoft.AspNetCore.Components;
using BusinessLogic.Records;
using Presentation.ViewModels.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Presentation.Inherits.Components
{
    public class SourceFieldTreeBase : ComponentBase
    {
        [Parameter]
        public TemplateFieldDto Node { get; set; } 

        protected SourceFieldTreeViewModel ViewModel { get; } = new();

        protected override void OnParametersSet()
        {
            ViewModel.Bind(Node);
        }
    }
}
