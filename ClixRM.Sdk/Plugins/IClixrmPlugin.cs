using Microsoft.Extensions.DependencyInjection;

namespace ClixRM.Sdk.Plugins;

/// <summary>
///     Contract for ClixRM Plugins. Plugins must implement this interface to expose commands and register
///     their services. 
/// </summary>
public interface IClixrmPlugin
{
    /// <summary>
    ///     Allows the plugin to register its own services (commands, helpers, etc.) with the host's DI container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    void ConfigureServices(IServiceCollection services);
    
    /// <summary>
    ///     Returns the type of the plugin's main command.
    /// </summary>
    /// <returns><see cref="Type"/> of plugin's main command.</returns>
    Type GetCommandType();
}