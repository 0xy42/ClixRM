using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClixRM.Models;

public class AppRegistrationConnectionDetailsUnsecure
{
    public AppRegistrationConnectionDetailsUnsecure(
        Guid connectionId,
        string environmentName,
        Guid clientId,
        string url,
        DateTime expiry)
    {
        ConnectionId = connectionId;
        EnvironmentName = environmentName;
        ClientId = clientId;
        Url = url;
        Expiry = expiry;
    }

    public Guid ConnectionId { get; set; }
    public string EnvironmentName { get; set; }
    public Guid ClientId { get; set; }
    public string Url { get; set; }
    public DateTime Expiry {  get; set; }

    public override string ToString()
    {
        return $"Environment: {EnvironmentName} | ClientId: {ClientId} | Url: {Url} | Expiry: {Expiry}";
    }
}
