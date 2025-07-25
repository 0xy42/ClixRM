using ClixRM.Sdk.Models;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace ClixRM.Sdk.Services;

/// <summary>
///     Service for Dataverse connection. Used by plugins and services that need to directly connect with the CRM.
/// </summary>
public interface IDataverseConnector
{
    /// <summary>
    ///     Get the used Connection, currently set as active in ClixRM.
    /// </summary>
    /// <returns>The used connection, currently set as active in CRM.</returns>
    public ActiveConnectionIdentifier GetActiveConnectionIdentifier();
    
    /// <summary>
    ///     Get the ServiceClient's <see cref="IOrganizationServiceAsync2"/> representation.
    /// </summary>
    /// <returns>The ServiceClient's <see cref="IOrganizationServiceAsync2"/> representation.</returns>
    Task<IOrganizationServiceAsync2> GetServiceClientAsync();
}