using ClixRM.Models;
using Microsoft.Extensions.Logging;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ClixRM.Sdk.Models;
using ClixRM.Sdk.Services;

namespace ClixRM.Services.Authentication;

[SupportedOSPlatform("windows")]
public class SecureStorage : ISecureStorage
{
    private readonly string _storageFilePath;
    private readonly string _activeEnvironmentFilePath;
    private static readonly byte[] SEntropy = Encoding.UTF8.GetBytes("ClixRM.v2");
    private readonly ILogger<SecureStorage> _logger;

    private const string AppRootFolderName = "ClixRM";
    private const string SecureStorageSubFolderName = "SecureStorage";

    public SecureStorage(ILogger<SecureStorage> logger)
    {
        _logger = logger;

        if (!OperatingSystem.IsWindows())
        {
            var errorMsg = "SecureStorage currently relies on Windows DPAPI and is not supported on this platform.";
            _logger.LogError(errorMsg);
            throw new PlatformNotSupportedException(errorMsg);
        }

        var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var clixRmAppBasePath = Path.Combine(localAppDataPath, AppRootFolderName);
        var secureStoragePath = Path.Combine(clixRmAppBasePath, SecureStorageSubFolderName);

        Directory.CreateDirectory(secureStoragePath);

        _storageFilePath = Path.Combine(secureStoragePath, "connections.v3.dpapi");
        _activeEnvironmentFilePath = Path.Combine(secureStoragePath, "active_connection_identifier.v2.json");
    }

    public void SaveConnection(ConnectionDetails connection)
    {
        var connections = LoadAllConnections();
        connections[connection.EnvironmentName.ToLower()] = connection;

        var json = JsonSerializer.Serialize(connections);
        var encryptedData = EncryptData(json);

        File.WriteAllBytes(_storageFilePath, encryptedData);
        _logger.LogInformation("Connection for environment '{EnvironmentName} saved.'", connection.EnvironmentName);
    }

    public ConnectionDetails GetConnection(string name)
    {
        var connections = LoadAllConnections();
        if (connections.TryGetValue(name.ToLower(), out var connection))
        {
            return connection;
        }

        _logger.LogWarning("No connection found for environment {EnvironmentName}.", name);
        throw new KeyNotFoundException($"No connection found for environment {name}");
    }

    public IEnumerable<ConnectionDetailsUnsecure> ListConnectionsUnsecure()
    {
        var connections = LoadAllConnections();
        return connections.Values.Select(c => c switch
        {
            UserConnectionDetails user => new ConnectionDetailsUnsecure(user.EnvironmentName, user.Url, user.ConnectionType, user.UserPrincipalName),
            AppSecretConnectionDetails app => new ConnectionDetailsUnsecure(app.EnvironmentName, app.Url, app.ConnectionType, $"App ID: {app.ClientId}"),
            _ => new ConnectionDetailsUnsecure(c.EnvironmentName, c.Url, "Unknown", "N/A")
        });
    }

    public IDictionary<string, ConnectionDetails> LoadAllConnections()
    {
        if (!File.Exists(_storageFilePath))
        {
            return new Dictionary<string, ConnectionDetails>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var encryptedData = File.ReadAllBytes(_storageFilePath);
            if (encryptedData.Length == 0) return new Dictionary<string, ConnectionDetails>(StringComparer.OrdinalIgnoreCase);

            var json = DecryptData(encryptedData);
            var deserialized = JsonSerializer.Deserialize<Dictionary<string, ConnectionDetails>>(json);

            return deserialized == null
                ? new Dictionary<string, ConnectionDetails>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, ConnectionDetails>(deserialized, StringComparer.OrdinalIgnoreCase);
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Error decrypting connection file '{StorageFilePath}'. Data may be corrupted or inaccessible.", _storageFilePath);
            return new Dictionary<string, ConnectionDetails>(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing decrypted connection data from '{StorageFilePath}'. File may be corrupted.", _storageFilePath);
            return new Dictionary<string, ConnectionDetails>(StringComparer.OrdinalIgnoreCase);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Error reading connection file '{StorageFilePath}'.", _storageFilePath);
            return new Dictionary<string, ConnectionDetails>(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred.");
            return new Dictionary<string, ConnectionDetails>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public void RemoveConnection(string name)
    {
        var connections = LoadAllConnections();
        var lowerEnvironmentName = name.ToLower();

        if (!connections.Remove(lowerEnvironmentName))
        {
            _logger.LogInformation("Attempted to remove non-existent connection for {EnvironmentName}.", name);
            return;
        }

        var activeIdentifier = GetActiveConnectionIdentifier();
        if (activeIdentifier?.EnvironmentName.Equals(lowerEnvironmentName, StringComparison.OrdinalIgnoreCase) == true)
        {
            if (File.Exists(_activeEnvironmentFilePath))
            {
                File.Delete(_activeEnvironmentFilePath);
                _logger.LogInformation("Active connection identifier file deleted as it matched removed environment {EnvironmentName}.", name);
            }
        }

        if (connections.Count == 0)
        {
            if (File.Exists(_storageFilePath)) File.Delete(_storageFilePath);
            _logger.LogInformation("All connection removed, storage file deleted.");
        }
        else
        {
            var json = JsonSerializer.Serialize(connections);
            var encryptedData = EncryptData(json);
            File.WriteAllBytes(_storageFilePath, encryptedData);
        }

        _logger.LogInformation("Connection for environment {EnvironmentName} removed.", name);
    }

    public void RemoveAllConnections()
    {
        if (File.Exists(_storageFilePath)) File.Delete(_storageFilePath);
        if (File.Exists(_activeEnvironmentFilePath)) File.Delete(_activeEnvironmentFilePath);
        _logger.LogInformation("All connections and active identifier removed.");
    }

    public void SetActiveEnvironment(string environmentName)
    {
        var lowerEnvironmentName = environmentName.ToLower();
        var connection = GetConnection(lowerEnvironmentName);
        var activeIdentifier = new ActiveConnectionIdentifier(lowerEnvironmentName, connection.ConnectionId);
        var json = JsonSerializer.Serialize(activeIdentifier);
        File.WriteAllText(_activeEnvironmentFilePath, json);
        _logger.LogInformation("Active environment set to {EnvironmentName}.", environmentName);
    }

    public ActiveConnectionIdentifier? GetActiveConnectionIdentifier()
    {
        if (!File.Exists(_activeEnvironmentFilePath)) return null;

        try
        {
            var json = File.ReadAllText(_activeEnvironmentFilePath);
            return string.IsNullOrEmpty(json)
                ? null
                : JsonSerializer.Deserialize<ActiveConnectionIdentifier>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading/deserializing active connection identifier from '{ActiveEnvFilePath}.", _activeEnvironmentFilePath);
            return null;
        }
    }

    private byte[] EncryptData(string plainText)
    {
        var dataBytes = Encoding.UTF8.GetBytes(plainText);
        return ProtectedData.Protect(dataBytes, SEntropy, DataProtectionScope.CurrentUser);
    }

    private string DecryptData(byte[] encryptedData)
    {
        var dataBytes = ProtectedData.Unprotect(encryptedData, SEntropy, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(dataBytes);
    }

    public bool DoesActiveConnectionExist()
    {
        if (!OperatingSystem.IsWindows()) return false;

        var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var clixRmAppBasePath = Path.Combine(localAppDataPath, AppRootFolderName);
        var secureStoragePath = Path.Combine(clixRmAppBasePath, SecureStorageSubFolderName);
        var activeEnvironmentFilePath = Path.Combine(secureStoragePath, "active_connection_identifier.v2.json");

        try
        {
            if (!File.Exists(activeEnvironmentFilePath)) return false;
            var json = File.ReadAllText(activeEnvironmentFilePath);
            if (string.IsNullOrWhiteSpace(json)) return false;
            var identifier = JsonSerializer.Deserialize<ActiveConnectionIdentifier>(json);
            return identifier != null && !string.IsNullOrWhiteSpace(identifier.EnvironmentName) && identifier.ConnectionId != Guid.Empty;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error checking active connection: {ex.Message}");
            return false;
        }
    }
}