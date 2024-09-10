using CourtDocumentProcessor.Interfaces;
using CourtDocumentProcessor;
using System.IO.Compression;
using System.Xml;
using System.Xml.Schema;

public class ZipFileProcessor
{
  private readonly ILogger _logger;
  private readonly AppSettings _config;
  private readonly IEmailNotifier _emailNotifier;
  private readonly XmlSchemaSet _schemaSet;

  private const string DefaultXsdSchema = @"
<xs:schema attributeFormDefault=""unqualified"" elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
  <xs:element name=""party"">
    <xs:complexType>
      <xs:sequence>
        <xs:element type=""xs:string"" name=""name""/>
        <xs:element type=""xs:string"" name=""email""/>
        <xs:element type=""xs:int"" name=""applicationno""/>
      </xs:sequence>
    </xs:complexType>
  </xs:element>
</xs:schema>";
  public ZipFileProcessor(ILogger logger, AppSettings config, IEmailNotifier emailNotifier)
  {
    _logger = logger;
    _config = config;
    _emailNotifier = emailNotifier;
    _schemaSet = new XmlSchemaSet();
    LoadXmlSchema();
  }

  private void LoadXmlSchema()
  {
    try
    {
      if (!string.IsNullOrEmpty(_config.XmlSchemaPath) && File.Exists(_config.XmlSchemaPath))
      {
        _schemaSet.Add(null, _config.XmlSchemaPath);
        _logger.Log($"Loaded XML schema from file: {_config.XmlSchemaPath}");
      }
      else
      {
        using (var stringReader = new StringReader(DefaultXsdSchema))
        {
          _schemaSet.Add(null, XmlReader.Create(stringReader));
        }
        _logger.Log("Using default XML schema");
      }
    }
    catch (Exception ex)
    {
      _logger.Log($"Error loading XML schema: {ex.Message}. Using default schema.");
      using (var stringReader = new StringReader(DefaultXsdSchema))
      {
        _schemaSet.Add(null, XmlReader.Create(stringReader));
      }
    }
  }

  private bool ValidateXmlAgainstSchema(string xmlFilePath)
  {
    try
    {
      XmlDocument doc = new XmlDocument();
      doc.Load(xmlFilePath);

      doc.Schemas = _schemaSet;
      doc.Validate((sender, e) =>
      {
        if (e.Severity == XmlSeverityType.Error)
        {
          throw new XmlSchemaValidationException(e.Message);
        }
      });

      return true;
    }
    catch (XmlSchemaValidationException ex)
    {
      _logger.Log($"XML validation error: {ex.Message}");
      return false;
    }
    catch (Exception ex)
    {
      _logger.Log($"Error validating XML: {ex.Message}");
      return false;
    }
  }
  public bool ProcessZipFile(string zipFilePath)
  {
    _logger.Log($"Starting to process ZIP file: {zipFilePath}");

    try
    {
      if (!File.Exists(zipFilePath))
      {
        _logger.Log($"Error: ZIP file does not exist at path: {zipFilePath}");
        return false;
      }

      if (!IsValidZipFile(zipFilePath))
      {
        _logger.Log("Error: Invalid ZIP file");
        _emailNotifier.SendNotification("Error processing ZIP file", "The ZIP file is invalid");
        return false;
      }

      string tempExtractPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
      Directory.CreateDirectory(tempExtractPath);

      _logger.Log($"Extracting ZIP file to temporary location: {tempExtractPath}");
      ZipFile.ExtractToDirectory(zipFilePath, tempExtractPath);

      string applicationNo = GetApplicationNoFromXml(tempExtractPath);
      string guid = Guid.NewGuid().ToString();
      string finalExtractPath = Path.Combine(_config.ExtractedFolderLocation, $"{applicationNo}-{guid}");

      _logger.Log($"Moving extracted files to final location: {finalExtractPath}");
      Directory.Move(tempExtractPath, finalExtractPath);

      _logger.Log("ZIP file extracted successfully");
      _emailNotifier.SendNotification("ZIP file processed successfully", $"File extracted to: {finalExtractPath}");

      return true;
    }
    catch (Exception ex)
    {
      _logger.Log($"Error in ProcessZipFile: {ex.GetType().Name} - {ex.Message}");
      _logger.Log($"Stack Trace: {ex.StackTrace}");
      _emailNotifier.SendNotification("Error processing ZIP file", ex.Message);
      return false;
    }
  }

  private bool IsValidZipFile(string zipFilePath)
  {
    try
    {
      using (ZipArchive archive = ZipFile.OpenRead(zipFilePath))
      {
        bool hasPartyXml = false;
        var allowedExtensions = _config.AllowedFileTypes.Split(',');

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
          string extension = Path.GetExtension(entry.FullName).TrimStart('.').ToLower();

          if (entry.Name.Equals("party.xml", StringComparison.OrdinalIgnoreCase))
          {
            hasPartyXml = true;
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

      return true;
    }
    catch (Exception ex)
    {
      _logger.Log($"Error validating ZIP file: {ex.Message}");
      return false;
    }
  }

  private string GetApplicationNoFromXml(string extractPath)
  {
    string partyXmlPath = Path.Combine(extractPath, "party.xml");
    _logger.Log($"Reading ApplicationNo from: {partyXmlPath}");

    if (!File.Exists(partyXmlPath))
    {
      throw new FileNotFoundException("party.xml not found in extracted files");
    }

    XmlDocument doc = new XmlDocument();
    doc.Load(partyXmlPath);

    XmlNode applicationNoNode = doc.SelectSingleNode("//party/applicationno");
    if (applicationNoNode == null)
    {
      throw new InvalidOperationException("ApplicationNo not found in party.xml");
    }

    string applicationNo = applicationNoNode.InnerText;
    _logger.Log($"ApplicationNo found: {applicationNo}");
    return applicationNo;
  }
}
