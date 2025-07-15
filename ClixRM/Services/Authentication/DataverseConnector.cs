using ClixRM.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace ClixRM.Services.Authentication;

public class DataverseConnector : IDataverseConnector
{
    private const string PublicClientId = "00533523-8d5b-4d10-909f-af554dec0546";

    private readonly ISecureStorage _secureStorage;
    private readonly ILogger<DataverseConnector> _logger;
    private ActiveConnectionIdentifier? _cachedActiveIdentifier;

    public DataverseConnector(
        ISecureStorage secureStorage,
        ILogger<DataverseConnector> logger)
    {
        _secureStorage = secureStorage;
        _logger = logger;
    }

    public async Task<IOrganizationServiceAsync2> GetServiceClientAsync()
    {
        var activeIdentifier = GetActiveConnectionIdentifier();
        var connectionDetails = _secureStorage.GetConnection(activeIdentifier.EnvironmentName);

        string accessToken;

        switch (connectionDetails)
        {
            case AppSecretConnectionDetails appDetails: 
                _logger.LogInformation("Acquiring token for App Registration connection: {Name}", appDetails.EnvironmentName);
                accessToken = await GetTokenForAppAsync(appDetails);
                break;

            case UserConnectionDetails userDetails:
                _logger.LogInformation("Acquiring token for User connection: {Name}", userDetails.EnvironmentName);
                accessToken = await GetTokenForUserAsync(userDetails);
                break;

            default:
                var errorMsg = $"Unsupported connection type found for environment {connectionDetails.EnvironmentName}";
                _logger.LogError(errorMsg);
                throw new NotSupportedException(errorMsg);
        }

        try
        {
            var serviceClient = new ServiceClient(
                instanceUrl: new Uri(connectionDetails.Url),
                tokenProviderFunction: _ => Task.FromResult(accessToken),
                useUniqueInstance: true,
                logger: _logger
            );

            if (!serviceClient.IsReady)
            {
                _logger.LogError("Failed to connect to Dataverse. ServiceClient LastError: {Error}", serviceClient.LastError);
                throw new InvalidOperationException($"Failed to connect to Dataverse environment {connectionDetails.EnvironmentName}: {serviceClient.LastError}");
            }

            _logger.LogInformation("ServiceClient successfully created and ready for environment {EnvironmentName}.", connectionDetails.EnvironmentName);
            
            return serviceClient;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize ServiceClient for environment {EnvironmentName}.", connectionDetails.EnvironmentName);
            throw;
        }
    }

    private async Task<string> GetTokenForAppAsync(AppSecretConnectionDetails appDetails)
    {
        var app = ConfidentialClientApplicationBuilder.Create(appDetails.ClientId.ToString())
            .WithClientSecret(appDetails.ClientSecret)
            .WithAuthority(new Uri($"https://login.microsoftonline.com/{appDetails.TenantId}"))
            .Build();

        var scopes = new[] { $"{appDetails.Url.TrimEnd('/')}/.default" };
        var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();

        return result.AccessToken;
    }

    private async Task<string> GetTokenForUserAsync(UserConnectionDetails userDetails)
    {
        var builder = PublicClientApplicationBuilder.Create(PublicClientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, userDetails.TenantId)
            .WithRedirectUri("http://localhost");

        var app = TokenCacheProvider.CreateClientApplication(builder);

        var validatedUrl = userDetails.Url.TrimEnd('/') + "/";
        var scopes = new[] { $"{validatedUrl}.default" };

        try
        {
            var account = await app.GetAccountAsync(userDetails.HomeAccountId);
            if (account == null)
            {
                _logger.LogWarning("Could not find cached account for {User}. User will need to log in again.", userDetails.UserPrincipalName);
                throw new InvalidOperationException($"Your cached login for {userDetails.UserPrincipalName} could not be found. Please log in again.");
            }

            var result = await app.AcquireTokenSilent(scopes, account).ExecuteAsync();

            return result.AccessToken;
        }
        catch (MsalUiRequiredException ex)
        {
            _logger.LogError(ex, "Silent token acquisition failed for {User}. Interactive login is required.", userDetails.UserPrincipalName);
            throw new InvalidOperationException($"Your session for {userDetails.UserPrincipalName} has expired or requires re-authentication. Please log in again.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred.");
            throw new InvalidOperationException($"An unexpected error occured while acquiring the token: {ex.Message}");
        }
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
}