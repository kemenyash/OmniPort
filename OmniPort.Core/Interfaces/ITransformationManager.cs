using OmniPort.Core.Enums;
using OmniPort.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Core.Interfaces
{
    public interface ITransformationManager
    {
        Task<(ImportProfile Profile, SourceType ImportSourceType, SourceType ConvertSourceType)> GetImportProfileForJoinAsync(int mappingTemplateId);
    }
}
