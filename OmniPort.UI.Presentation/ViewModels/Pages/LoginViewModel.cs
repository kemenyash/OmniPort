using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.ViewModels.Pages
{
    public class LoginViewModel
    {
        public bool ShowInvalidCredentials { get; private set; }

        public void SetInvalidCredentials(bool value)
        {
            ShowInvalidCredentials = value;
        }
    }
}
