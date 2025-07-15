using ClixRM.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Newtonsoft.Json;

namespace ClixRM.Services.Authentication;

public class AuthService : IAuthService
{
    private const string PublicClientId = "00533523-8d5b-4d10-909f-af554dec0546";

    private readonly ILogger<AuthService> _logger;
    private static readonly HttpClient _httpClient = new();

    public AuthService(ILogger<AuthService> logger)
    {
        _logger = logger;
    }

    [Obsolete("A new method exists, that does not require a Tenant ID to be passed.")]
    public async Task<AppRegistrationConnectionDetailsSecure> AuthenticateAsync(
        Guid clientId, string clientSecret, Guid tenantId, string url, string connectionName)
    {
        _logger.LogInformation("Attempting to authenticate for Client ID: {ClientId}, Tenant ID: {TenantId}, URL: {Url}, Connection Name: {ConnectionName}",
            clientId, tenantId, url, connectionName);

        if (string.IsNullOrWhiteSpace(url))
        {
            _logger.LogError("Authentication failed: URL cannot be empty.");
            throw new ArgumentNullException(nameof(url), "Environment URL cannot be empty.");
        }
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            _logger.LogError("Authentication failed: Invalid URL format for {Url}.", url);
            throw new ArgumentException("Invalid URL format.", nameof(url));
        }

        try
        {
            var app = ConfidentialClientApplicationBuilder.Create(clientId.ToString())
                .WithClientSecret(clientSecret)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
                .Build();

            var validatedUrl = url.EndsWith("/") ? url : $"{url}/";
            var scopes = new[] { $"{validatedUrl}.default" };

            _logger.LogInformation("Acquiring token with scopes: {Scopes}", string.Join(", ", scopes));

            var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();

            _logger.LogInformation("Token acquired successfully. Expires on: {Expiry}", result.ExpiresOn.UtcDateTime);

            return new AppRegistrationConnectionDetailsSecure(
                connectionId: Guid.NewGuid(),
                environmentName: connectionName.ToLower(),
                clientId: clientId,
                clientSecret: clientSecret,
                tenantId: tenantId,
                accessToken: result.AccessToken,
                expiry: result.ExpiresOn.UtcDateTime,
                url: url
            );
        }
        catch (MsalServiceException ex)
        {
            _logger.LogError(ex, "MSAL Service Exception during authentication for Client ID {ClientId}. ErrorCode: {ErrorCode}. Message: {Message}", clientId, ex.ErrorCode, ex.Message);
            throw new InvalidOperationException($"Authentication failed: {ex.Message} (MSAL Error: {ex.ErrorCode})", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Generic exception during authentication for Client ID {ClientId}.", clientId);
            throw new InvalidOperationException($"An unexpected error occurred during authentication: {ex.Message}", ex);
        }
    }

    public async Task<AppRegistrationConnectionDetailsSecure> AuthenticateAppAsync(
        Guid clientId, string clientSecret, string url, string connectionName)
    {
        _logger.LogInformation("Attempting to authenticate for Client ID: {ClientId}, URL: {Url}, Connection Name: {ConnectionName}",
            clientId, url, connectionName);

        if (string.IsNullOrWhiteSpace(url))
        {
            _logger.LogError("Authentication failed: URL cannot be empty.");
            throw new ArgumentNullException(nameof(url), "Environment URL cannot be empty.");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            _logger.LogError("Authentication failed: Invalid URL format for {Url}.", url);
            throw new ArgumentException("Invalid URL format.", nameof(url));
        }

        try
        {
            _logger.LogInformation("Discovering Tenant ID from URL: {Url}", url);
            var tenantId = await DiscoverTenantIdAsync(url);

            if (tenantId == null)
            {
                _logger.LogError("Failed to discover Tenant ID from URL: {Url}", url);
                throw new InvalidOperationException("Could not discover Tenant ID from the provided environment URL. Please verify the URL is correct.");
            }

            _logger.LogInformation("Successfully discovered Tenant ID: {TenantId}", tenantId);

            var app = ConfidentialClientApplicationBuilder.Create(clientId.ToString())
                .WithClientSecret(clientSecret)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
                .Build();

            var validateUrl = url.EndsWith("/") ? url : $"{url}/";
            var scopes = new[] { $"{validateUrl}.default" };

            _logger.LogInformation("Acquiring token with scopes: {Scopes}", string.Join(", ", scopes));

            var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();

            _logger.LogInformation("Token acquired successfully. Expires on: {Expiry}", result.ExpiresOn.UtcDateTime);

            return new AppRegistrationConnectionDetailsSecure(
                connectionId: Guid.NewGuid(),
                environmentName: connectionName.ToLower(),
                clientId: clientId,
                clientSecret: clientSecret,
                tenantId: tenantId.Value,
                accessToken: result.AccessToken,
                expiry: result.ExpiresOn.UtcDateTime,
                url: url);
        }
        catch (MsalServiceException ex)
        {
            _logger.LogError(ex, "MSAL Service Exception during authentication for Client ID {ClientId}. ErrorCode: {ErrorCode}. Message: {Message}", clientId, ex.ErrorCode, ex.Message);
            throw new InvalidOperationException($"Authentication failed: {ex.Message} (MSAL Error: {ex.ErrorCode})", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Generic exception during authentication for Client ID {ClientId}.", clientId);
            throw new InvalidOperationException($"An unexpected error occurred during authentication: {ex.Message}", ex);
        }
    }

    public async Task<UserConnectionDetails> AuthenticateWithUserAsync(string url, string connectionName)
    {
        _logger.LogInformation("Attempting interactive user authentication for URL: {Url}", url);

        if (!Guid.TryParse(PublicClientId, out var publicClientIdGuid))
        {
            const string errorMsg = "The application is not configured correctly for user authentication. The Public Client ID is invalid.";
            _logger.LogError(errorMsg);
            throw new InvalidOperationException(errorMsg);
        }

        try
        {
            var app = PublicClientApplicationBuilder.Create(PublicClientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, "organizations")
                .WithRedirectUri("http://localhost")
                .Build();

            var validateUrl = url.EndsWith("/") ? url : $"{url}/";
            var scopes = new[] { $"{validateUrl}.default" };

            _logger.LogInformation("Acquiring token interactively with scopes: {Scopes}", string.Join(", ", scopes));

            AuthenticationResult result = await app.AcquireTokenInteractive(scopes).ExecuteAsync();

            var authenticatedUserAccount = result.Account;
            var userPrincipalName = authenticatedUserAccount.Username;
            var tenantId = Guid.Parse(result.TenantId);

            return new UserConnectionDetails(
                connectionId: Guid.NewGuid(),
                environmentName: connectionName.ToLower(),
                url: url,
                tenantId: tenantId,
                clientId: publicClientIdGuid,
                userPrincipalName: userPrincipalName);
        }
        catch (MsalClientException ex) when (ex.ErrorCode == "authentication_canceled")
        {
            _logger.LogWarning("User canceled the authentication process in the browser.");
            throw new OperationCanceledException("Authentication was canceled by the user.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during interactive authentication.");
            throw new InvalidOperationException($"An unexpected error occurred during authentication: {ex.Message}", ex);
        }
    }

    public async Task<Guid?> DiscoverTenantIdAsync(string dynamicsUrl)
    {
        var discoveryRequestUrl = new Uri(new Uri(dynamicsUrl), "api/data/v9.2/RetrieveCurrentOrganization(AccessType='Default')");

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, discoveryRequestUrl);

            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (response.Headers.TryGetValues("WWW-Authenticate", out var headerValues))
                {
                    var wwwAuthHeader = headerValues.FirstOrDefault();
                    if (!string.IsNullOrEmpty(wwwAuthHeader))
                    {
                        var parameters = new WwwAuthenticateParameters(wwwAuthHeader);
                        return parameters.GetTenantId();
                    }
                }
            }

            _logger.LogWarning("Did not receive a 401 Unauthorized with a WWW-Authenticate header. Statuscode: {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "A network error occurred while trying to discover the Tenant ID from {Url}", dynamicsUrl);
        }

        return null;
    }
}