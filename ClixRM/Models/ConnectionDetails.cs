using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ClixRM.Models
{
    [JsonDerivedType(typeof(AppSecretConnectionDetails), typeDiscriminator: "app")]
    [JsonDerivedType(typeof(UserConnectionDetails), typeDiscriminator: "user")]
    public abstract record ConnectionDetails
    {
        public Guid ConnectionId { get; init; }
        public string EnvironmentName { get; init; }
        public string Url { get; init; }
        public Guid TenantId { get; init; }
        public Guid ClientId { get; init; }

        public abstract string ConnectionType { get; }

        protected ConnectionDetails(Guid connectionId, string environmentName, string url, Guid tenantId, Guid clientId)
        {
            ConnectionId = connectionId;
            EnvironmentName = environmentName;
            Url = url;
            TenantId = tenantId;
            ClientId = clientId;
        }
    }

    public record AppSecretConnectionDetails : ConnectionDetails
    {
        public string ClientSecret { get; }
        public override string ConnectionType => "App Registration (Secret)";

        public AppSecretConnectionDetails(Guid connectionId, string environmentName, string url, Guid tenantId, Guid clientId, string clientSecret)
            : base(connectionId, environmentName, url, tenantId, clientId)
        {
            ClientSecret = clientSecret;
        }
    }

    public record UserConnectionDetails : ConnectionDetails
    {
        public string UserPrincipalName { get; }
        public override string ConnectionType => "User Account";

        public UserConnectionDetails(Guid connectionId, string environmentName, string url, Guid tenantId, Guid clientId, string userPrincipalName) 
            : base(connectionId, environmentName, url, tenantId, clientId)
        {
            UserPrincipalName = userPrincipalName;
        }
    }
}
