using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.ViewModels.Pages
{
    public class IndexViewModel
    {
        public bool ShowTemplates { get; private set; }
        public bool ShowJoinTemplates { get; private set; }

        public void OpenTemplates()
        {
            ShowTemplates = true;
        }

        public void CloseTemplates()
        {
            ShowTemplates = false;
        }

        public void OpenJoinTemplates()
        {
            ShowJoinTemplates = true;
        }

        public void CloseJoinTemplates()
        {
            ShowJoinTemplates = false;
        }
    }
}
