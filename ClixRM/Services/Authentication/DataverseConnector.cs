using ClixRM.Models;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace ClixRM.Services.Authentication;

public class DataverseConnector : IDataverseConnector
{
    private readonly ISecureStorage _secureStorage;
    private readonly IAuthService _authService;
    private readonly ILogger<DataverseConnector> _logger;
    private ActiveConnectionIdentifier? _cachedActiveIdentifier;

    public DataverseConnector(
        ISecureStorage secureStorage,
        IAuthService authService,
        ILogger<DataverseConnector> logger)
    {
        _secureStorage = secureStorage;
        _authService = authService;
        _logger = logger;
    }

    public ActiveConnectionIdentifier GetActiveConnectionIdentifier()
    {
        _cachedActiveIdentifier ??= _secureStorage.GetActiveConnectionIdentifier();

        if (_cachedActiveIdentifier == null)
        {
            _logger.LogWarning("Attempted to get active connection identifier, but none was set.");
            throw new InvalidOperationException("No active Dataverse connection is set. Please set an active environment first using 'clixrm auth set-active'.");
        }

        return _cachedActiveIdentifier;
    }

    public async Task<IOrganizationServiceAsync2> GetServiceClientAsync()
    {
        var activeIdentifier = GetActiveConnectionIdentifier();
        var connectionDetails = _secureStorage.GetConnection(activeIdentifier.EnvironmentName);

        if (connectionDetails.Expiry <= DateTime.UtcNow.AddMinutes(5))
        {
            _logger.LogInformation("Access token for environment '{EnvironmentName}' (ID: {ConnectionId}) is expired or nearing expiry (Expiry: {ExpiryTime}). Attempting to refresh.",
                activeIdentifier.EnvironmentName, activeIdentifier.ConnectionId, connectionDetails.Expiry);

            try
            {
                var newConnectionDetails = await _authService.AuthenticateAsync(
                    connectionDetails.ClientId,
                    connectionDetails.ClientSecret,
                    connectionDetails.TenantId,
                    connectionDetails.Url,
                    connectionDetails.EnvironmentName
                );

                newConnectionDetails.ConnectionId = connectionDetails.ConnectionId;

                _secureStorage.SaveConnection(newConnectionDetails);
                connectionDetails = newConnectionDetails;
                _logger.LogInformation("Token refreshed successfully for environment '{EnvironmentName}'. New expiry: {NewExpiryTime}",
                    activeIdentifier.EnvironmentName, connectionDetails.Expiry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh token for environment '{EnvironmentName}' (ID: {ConnectionId}).",
                    activeIdentifier.EnvironmentName, activeIdentifier.ConnectionId);
                throw new InvalidOperationException($"The access token for '{activeIdentifier.EnvironmentName}' has expired, and an attempt to refresh it failed: {ex.Message}", ex);
            }
        }

        try
        {
            _logger.LogDebug("Creating ServiceClient for environment '{EnvironmentName}' with URL '{Url}'. Token Expiry: {ExpiryTime}",
                activeIdentifier.EnvironmentName, connectionDetails.Url, connectionDetails.Expiry);

            var serviceClient = new ServiceClient(
                instanceUrl: new Uri(connectionDetails.Url),
                tokenProviderFunction: async (_) => await Task.FromResult(connectionDetails.AccessToken),
                useUniqueInstance: true
            );

            if (!serviceClient.IsReady)
            {
                _logger.LogError("Failed to connect to Dataverse environment '{EnvironmentName}' (ID: {ConnectionId}). ServiceClient LastError: {Error}",
                    activeIdentifier.EnvironmentName, activeIdentifier.ConnectionId, serviceClient.LastError);
                throw new InvalidOperationException($"Failed to connect to Dataverse environment '{activeIdentifier.EnvironmentName}' (ID: {activeIdentifier.ConnectionId}): {serviceClient.LastError}");
            }
            _logger.LogInformation("ServiceClient successfully created and ready for environment '{EnvironmentName}'.", activeIdentifier.EnvironmentName);

            return serviceClient;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize ServiceClient for environment '{EnvironmentName}' (ID: {ConnectionId}).",
                activeIdentifier.EnvironmentName, activeIdentifier.ConnectionId);
            throw new InvalidOperationException($"Failed to initialize ServiceClient for environment '{activeIdentifier.EnvironmentName}' (ID: {activeIdentifier.ConnectionId}): {ex.Message}", ex);
        }
    }
}