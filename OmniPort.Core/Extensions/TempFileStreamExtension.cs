using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Core.Extensions
{
    public class TempFileStreamExtension : FileStream
    {
        private readonly string path;
        public TempFileStreamExtension(string path)
            : base(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: false)
        {
            this.path = path;
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            try { File.Delete(path); } catch { }
        }
    }
}


