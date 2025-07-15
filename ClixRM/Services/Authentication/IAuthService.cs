using ClixRM.Models;

namespace ClixRM.Services.Authentication
{
    public interface IAuthService
    {
        [Obsolete("A new version that doesnt require tenantId can be used instead.")]
        Task<AppRegistrationConnectionDetailsSecure> AuthenticateAsync(
            Guid clientId,
            string clientSecret,
            Guid tenantId,
            string url,
            string connectionName
        );

        Task<AppRegistrationConnectionDetailsSecure> AuthenticateAppAsync(
            Guid clientId,
            string clientSecret,
            string url,
            string connectionName
        );

        Task<UserConnectionDetails> AuthenticateWithUserAsync(
            string url, string connectionName);
    }
}
