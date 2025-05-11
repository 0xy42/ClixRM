namespace ClixRM.Models;

public class AppRegistrationConnectionDetails
{
    public Guid ConnectionId { get; set; }
    public string EnvironmentName { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string TenantId { get; set; }
    public string AccessToken { get; set; }
    public DateTime Expiry { get; set; }
    public string Url { get; set; }

    public AppRegistrationConnectionDetails(
        Guid connectionId,
        string environmentName,
        string clientId,
        string clientSecret,
        string tenantId,
        string accessToken,
        DateTime expiry,
        string url)
    {
        ConnectionId = connectionId;
        EnvironmentName = environmentName;
        ClientId = clientId;
        ClientSecret = clientSecret;
        TenantId = tenantId;
        AccessToken = accessToken;
        Expiry = expiry;
        Url = url;
    }
}