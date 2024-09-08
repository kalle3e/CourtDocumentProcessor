using System.Collections.Generic;
using System.IO;

namespace CourtDocumentProcessor.Interfaces
{
    public interface IAppConfigManager
    {
        string GetSetting(string key);
    }
}
