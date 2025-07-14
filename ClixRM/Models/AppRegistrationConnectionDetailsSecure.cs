namespace ClixRM.Models;

public class AppRegistrationConnectionDetailsSecure
{
    public Guid ConnectionId { get; set; }
    public string EnvironmentName { get; set; }
    public Guid ClientId { get; set; }
    public string ClientSecret { get; set; }
    public Guid TenantId { get; set; }
    public string AccessToken { get; set; }
    public DateTime Expiry { get; set; }
    public string Url { get; set; }

    public AppRegistrationConnectionDetailsSecure(
        Guid connectionId,
        string environmentName,
        Guid clientId,
        string clientSecret,
        Guid tenantId,
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

    public AppRegistrationConnectionDetailsUnsecure ToUnsecure()
    {
        return new AppRegistrationConnectionDetailsUnsecure(ConnectionId, EnvironmentName, ClientId, Url, Expiry);
    }
}