using ClixRM.Models;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace ClixRM.Services.Authentication;

public interface IDataverseConnector
{
    public ActiveConnectionIdentifier GetActiveConnectionIdentifier();
    Task<IOrganizationServiceAsync2> GetServiceClientAsync();
}