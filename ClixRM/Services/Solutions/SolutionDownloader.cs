using ClixRM.Models;
using ClixRM.Services.Authentication;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.IO.Compression;
using System.ServiceModel;
using System.Text.Json;
using ClixRM.Sdk.Models;
using ClixRM.Sdk.Services;

namespace ClixRM.Services.Solutions;

public class SolutionDownloader : ISolutionDownloader
{
    private const string MetadataFileName = "clixrm_metadata.json";
    private readonly IDataverseConnector _dataverseConnector;
    private readonly string _solutionCacheBaseDirectory;

    private const string AppRootFolderName = "ClixRM";
    private const string SolutionCacheSubFolderName = "SolutionCache";

    public SolutionDownloader(IDataverseConnector dataverseConnector, IConfiguration configuration)
    {
        _dataverseConnector = dataverseConnector;

        var customSolutionCachePath = configuration["SolutionCachePath"];

        if (!string.IsNullOrWhiteSpace(customSolutionCachePath))
        {
            _solutionCacheBaseDirectory = customSolutionCachePath;
        }
        else
        {
            var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var clixRmAppBasePath = Path.Combine(localAppDataPath, AppRootFolderName);
            _solutionCacheBaseDirectory = Path.Combine(clixRmAppBasePath, SolutionCacheSubFolderName);
        }

        try
        {
            Directory.CreateDirectory(_solutionCacheBaseDirectory);
        }
        catch (Exception ex)
        {
            throw new SolutionDownloadException($"Failed to initialize solution cache base directory: {_solutionCacheBaseDirectory}", ex);
        }
    }

    public string GetSolutionCacheBasePath() => _solutionCacheBaseDirectory;

    private string GetConnectionSpecificSolutionCachePath(string solutionUniqueName, Guid connectionId)
    {
        if (connectionId == Guid.Empty)
        {
            throw new ArgumentException("Connection ID cannot be empty.", nameof(connectionId));
        }
        var connectionSpecificFolder = Path.Combine(_solutionCacheBaseDirectory, connectionId.ToString());

        return Path.Combine(connectionSpecificFolder, SanitizeFolderName(solutionUniqueName));
    }

    public SolutionDownloadResult? GetExistingSolutionInfo(string solutionUniqueName)
    {
        if (string.IsNullOrWhiteSpace(solutionUniqueName)) return null;
        ActiveConnectionIdentifier activeConnection;

        try
        {
            activeConnection = _dataverseConnector.GetActiveConnectionIdentifier();
        }
        catch (Exception) { throw; }

        var solutionDir = GetConnectionSpecificSolutionCachePath(solutionUniqueName, activeConnection.ConnectionId);
        var metadataPath = Path.Combine(solutionDir, MetadataFileName);

        if (!File.Exists(metadataPath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(metadataPath);
            var metadata = JsonSerializer.Deserialize<SolutionDownloadResult>(json);
            if (metadata != null && Directory.Exists(metadata.UnpackedSolutionPath) &&
                metadata.UnpackedSolutionPath.Equals(solutionDir, StringComparison.OrdinalIgnoreCase))
            {
                return metadata;
            }

            if (File.Exists(metadataPath))
            {
                File.Delete(metadataPath);
            }

            return null;
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            if (File.Exists(metadataPath))
                File.Delete(metadataPath);

            return null;
        }
    }
    public async Task<SolutionDownloadResult> DownloadAndUnpackSolutionAsync(string solutionUniqueName, bool forceDownload = false)
    {
        if (string.IsNullOrWhiteSpace(solutionUniqueName))
        {
            throw new ArgumentException("Solution unique name cannot be null or whitespace.", nameof(solutionUniqueName));
        }

        ActiveConnectionIdentifier activeConnection;
        IOrganizationServiceAsync2 serviceClient;

        try
        {
            activeConnection = _dataverseConnector.GetActiveConnectionIdentifier();
            serviceClient = await _dataverseConnector.GetServiceClientAsync();
        }
        catch (InvalidOperationException ex)
        {
            throw new SolutionDownloadException($"Cannot download solution: {ex.Message}", ex);
        }

        if (!forceDownload)
        {
            var existingInfo = GetExistingSolutionInfo(solutionUniqueName);
            if (existingInfo != null) return existingInfo;
        }

        var solutionDir = GetConnectionSpecificSolutionCachePath(solutionUniqueName, activeConnection.ConnectionId);
        var tempZipPath = Path.Combine(Path.GetTempPath(), $"{SanitizeFolderName(solutionUniqueName)}_{activeConnection.ConnectionId}_{Guid.NewGuid()}.zip");

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(solutionDir)!);

            if (Directory.Exists(solutionDir))
            {
                Directory.Delete(solutionDir, recursive: true);
            }

            Directory.CreateDirectory(solutionDir);

            var exportRequest = new ExportSolutionRequest { SolutionName = solutionUniqueName, Managed = false };
            var exportResponse = await Task.Run(() => (ExportSolutionResponse)serviceClient.Execute(exportRequest));
            var solutionZipBytes = exportResponse.ExportSolutionFile;

            await File.WriteAllBytesAsync(tempZipPath, solutionZipBytes);
            ZipFile.ExtractToDirectory(tempZipPath, solutionDir, overwriteFiles: true);
            var solutionVersion = await GetSolutionVersionFromDataverseAsync(serviceClient, solutionUniqueName);

            var result = new SolutionDownloadResult(
                SolutionUniqueName: solutionUniqueName,
                UnpackedSolutionPath: solutionDir,
                DownloadTimestampUtc: DateTime.UtcNow,
                SolutionVersion: solutionVersion
            );

            var metadataJson = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            var metadataPath = Path.Combine(solutionDir, MetadataFileName);
            await File.WriteAllTextAsync(metadataPath, metadataJson);

            return result;
        }
        catch (FaultException<OrganizationServiceFault> ex)
        {
            throw new SolutionDownloadException($"Dataverse service error exporting solution '{solutionUniqueName}' (Connection: {activeConnection.EnvironmentName}): {ex.Detail.Message}", ex);
        }
        catch (Exception ex)
        {
            try
            {
                if (Directory.Exists(solutionDir)) Directory.Delete(solutionDir, true);
            }
            catch
            {
                /* Ignore cleanup errors */
            }
            throw new SolutionDownloadException($"Failed to download and unpack solution '{solutionUniqueName}' (Connection: {activeConnection.EnvironmentName}). See inner exception for details.", ex);
        }
        finally
        {
            try { if (File.Exists(tempZipPath)) File.Delete(tempZipPath); } catch (Exception) { /* Log error */ }
        }
    }

    private async Task<string?> GetSolutionVersionFromDataverseAsync(IOrganizationServiceAsync2 service, string solutionUniqueName)
    {
        try
        {
            var query = new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet("version"),
                Criteria = new FilterExpression
                {
                    Conditions = { new ConditionExpression("uniquename", ConditionOperator.Equal, solutionUniqueName) }
                }
            };
            var results = await service.RetrieveMultipleAsync(query);
            if (results.Entities.Count > 0 && results.Entities[0].Contains("version"))
                return results.Entities[0].GetAttributeValue<string>("version");
        }
        catch (Exception)
        {
            /* Log error */
        }
        return null;
    }

    public bool ClearSolutionCache(string solutionUniqueName)
    {
        if (string.IsNullOrWhiteSpace(solutionUniqueName)) return false;
        ActiveConnectionIdentifier activeConnection;

        try
        {
            activeConnection = _dataverseConnector.GetActiveConnectionIdentifier();
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        var solutionDir = GetConnectionSpecificSolutionCachePath(solutionUniqueName, activeConnection.ConnectionId);

        if (!Directory.Exists(solutionDir)) return false;

        try
        {
            Directory.Delete(solutionDir, recursive: true);
            return true;
        }
        catch (Exception)
        {
            /* Log error */
            return false;
        }
    }

    public bool ClearEntireCache()
    {
        if (!Directory.Exists(_solutionCacheBaseDirectory)) return false;

        try
        {
            Directory.Delete(_solutionCacheBaseDirectory, recursive: true);
            Directory.CreateDirectory(_solutionCacheBaseDirectory);
            return true;
        }
        catch (Exception)
        {
            /* Log error */
            return false;
        }
    }

    private static string SanitizeFolderName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).Distinct().ToArray();
        return string.Join("_", name.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
    }
}