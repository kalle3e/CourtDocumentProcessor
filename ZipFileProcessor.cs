using System;
using System.IO;
using System.IO.Compression;
using System.Xml;
using System.Xml.Schema;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using CourtDocumentProcessor.Interfaces;
//using CourtDocumentProcessor.Implementations;
//using CourtDocumentProcessor.Implementations;

namespace CourtDocumentProcessor
{
    class ZipFileProcessor
    {
        private readonly ILogger _logger;
        private readonly AppSettings _config;
        private readonly IEmailNotifier _emailNotifier;

        public ZipFileProcessor(ILogger logger, AppSettings config, IEmailNotifier emailNotifier)
        {
            _logger = logger;
            _config = config;
            _emailNotifier = emailNotifier;
        }

        public void ProcessZipFile(string zipFilePath)
        {
            try
            {
                _logger.Log($"Processing ZIP file: {zipFilePath}");

                if (IsValidZipFile(zipFilePath))
                {
                       string extractPath = ExtractZipFile(zipFilePath);
                       _emailNotifier.SendNotification("ZIP file processed successfully", $"File extracted to: {extractPath}");
                }
                else
                {
                    _logger.Log("Invalid ZIP file");
                    _emailNotifier.SendNotification("Error processing ZIP file", "The ZIP file is invalid");
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"Error processing ZIP file: {ex.Message}");
                _emailNotifier.SendNotification("Error processing ZIP file", ex.Message);
            }
        }

        private bool IsValidZipFile(string zipFilePath)
        {
            if (!File.Exists(zipFilePath) ||
                !Path.GetExtension(zipFilePath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                _logger.Log("File does not exist or is not a ZIP file");
                return false;
            }

            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(zipFilePath))
                {
                    var allowedExtensions = _config.AllowedFileTypes.Split(',');
                    bool hasPartyXml = false;


                    // System.IO.Compression.ZipArchiveEntry
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string extension = Path.GetExtension(entry.FullName).TrimStart('.').ToLower();

                        if (entry.Name.Equals("party.xml", StringComparison.OrdinalIgnoreCase))
                        {
                            hasPartyXml = true;
                            if (!ValidateXmlAgainstSchema(entry))
                            {
                                _logger.Log("party.xml does not conform to the schema");
                                return false;
                            }
                        }
                        else if (!allowedExtensions.Contains(extension))
                        {
                            _logger.Log($"Invalid file type found: {entry.FullName}");
                            return false;
                        }
                    }

                    if (!hasPartyXml)
                    {
                        _logger.Log("party.xml not found in the ZIP file");
                        return false;
                    }
                }
            }
            catch (InvalidDataException)
            {
                _logger.Log("The ZIP file is corrupt");
                return false;
            }

            return true;
        }

        private bool ValidateXmlAgainstSchema(ZipArchiveEntry xmlEntry)
        {
            XmlSchemaSet schema = new XmlSchemaSet();
            schema.Add("", _config.XmlSchemaPath);

            using (XmlReader reader = XmlReader.Create(xmlEntry.Open()))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(reader);

                doc.Schemas = schema;
                doc.Validate((sender, e) =>
                {
                    if (e.Severity == XmlSeverityType.Error)
                    {
                        _logger.Log($"XML validation error: {e.Message}");
                        throw new XmlSchemaValidationException(e.Message);
                    }
                });
            }

            return true;
        }

        private string ExtractZipFile(string zipFilePath)
        {
            string applicationNo = GetApplicationNoFromXml(zipFilePath);
            string guid = Guid.NewGuid().ToString();
            string extractPath = Path.Combine(_config.ExtractedFolderLocation, $"{applicationNo}-{guid}");
            _logger.Log($"Extracting ZIP file to: {extractPath}");

            ZipFile.ExtractToDirectory(zipFilePath, extractPath);

            return extractPath;
        }

        private string GetApplicationNoFromXml(string zipFilePath)
        {
            using (ZipArchive archive = ZipFile.OpenRead(zipFilePath))
            {
                var partyXml = archive.GetEntry("party.xml");
                if (partyXml != null)
                {
                    using (var stream = partyXml.Open())
                    using (var reader = XmlReader.Create(stream))
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.Load(reader);
                        XmlNode applicationNoNode = doc.SelectSingleNode("//party/applicationno");
                        if (applicationNoNode != null)
                        {
                            return applicationNoNode.InnerText;
                        }
                    }
                }
            }

            throw new InvalidOperationException("ApplicationNo not found in party.xml");
        }

    }
}
