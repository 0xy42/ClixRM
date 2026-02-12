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
    ActiveConnectionIdentifier GetActiveConnectionIdentifier();
    
    /// <summary>
    ///     Get the ServiceClient's <see cref="IOrganizationServiceAsync2"/> representation.
    /// </summary>
    /// <returns>The ServiceClient's <see cref="IOrganizationServiceAsync2"/> representation.</returns>
    Task<IOrganizationServiceAsync2> GetServiceClientAsync();

    /// <summary>
    ///     Gets the ServiceClient's <see cref="IOrganizationServiceAsync2"/> representation for a specific connection name."/>
    /// </summary>
    /// <param name="connectionName">The stored connection's name to use.</param>
    /// <returns>The <see cref="IOrganizationServiceAsync2"/> instance for the specific environment.</returns>
    Task<IOrganizationServiceAsync2> GetServiceClientAsync(string connectionName);
}