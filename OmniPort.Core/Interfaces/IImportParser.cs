using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Core.Interfaces
{
    /// <summary>
    /// Interface for any file parser (JSON, Excel, XML etc.)
    /// </summary>
    public interface IImportParser
    {
        IEnumerable<IDictionary<string, object?>> Parse(Stream stream);
    }
}
