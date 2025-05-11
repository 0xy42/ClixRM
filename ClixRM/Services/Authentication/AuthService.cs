using ClixRM.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;

namespace ClixRM.Services.Authentication;

public class AuthService : IAuthService
{
    private readonly ILogger<AuthService> _logger;

    public AuthService(ILogger<AuthService> logger)
    {
        _logger = logger;
    }

    public async Task<AppRegistrationConnectionDetails> AuthenticateAsync(
        string clientId, string clientSecret, string tenantId, string url, string connectionName)
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
            var app = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithClientSecret(clientSecret)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
                .Build();

            var validatedUrl = url.EndsWith("/") ? url : $"{url}/";
            var scopes = new[] { $"{validatedUrl}.default" };

            _logger.LogInformation("Acquiring token with scopes: {Scopes}", string.Join(", ", scopes));

            var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();

            _logger.LogInformation("Token acquired successfully. Expires on: {Expiry}", result.ExpiresOn.UtcDateTime);

            return new AppRegistrationConnectionDetails(
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
}