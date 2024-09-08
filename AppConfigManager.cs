using System;
using CourtDocumentProcessor.Interfaces;

namespace CourtDocumentProcessor
{
    
    public class AppConfigManager : IAppConfigManager
    {
        private readonly AppSettings _config;

        public AppConfigManager(AppSettings config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public string GetSetting(string key)
        {
            return key switch
            {
                "AdminEmail" => _config.AdminEmail,
                "ExtractedFolderLocation" => _config.ExtractedFolderLocation,
                "AllowedFileTypes" => _config.AllowedFileTypes,
                "XmlSchemaPath" => _config.XmlSchemaPath,
                "ZipFilePath" => _config.ZipFilePath,
                _ => throw new ArgumentException($"Unknown key: {key}", nameof(key))
            };
        }
    }

    // not appSettings.json ?
    // In app.config:
    /*
    <?xml version="1.0" encoding="utf-8" ?>
    <configuration>
      <appSettings>
        <add key="AdminEmail" value="admin@example.com" />
        <add key="ExtractedFolderLocation" value="C:\ExtractedFiles" />
        <add key="AllowedFileTypes" value="xls,xlsx,doc,docx,pdf,msg,jpg,png,gif" />
        <add key="XmlSchemaPath" value="party.xsd" />
      </appSettings>
    </configuration>
    */
}
