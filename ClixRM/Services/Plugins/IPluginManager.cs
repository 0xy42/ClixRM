using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace ClixRM.Services.Plugins;

/// <summary>
///     Service responsible for discovering, loading, and registering plugins.
/// </summary>
public interface IPluginManager
{
    /// <summary>
    ///     Discovers all valid plugins, registers their services with the DI container,
    ///     and returns the command types that need to be added to the CLI.
    /// </summary>
    /// <param name="services">The service collection to register plugin services into.</param>
    /// <returns>A read-only list of the main <see cref="Type"/> for each discovered plugin command.</returns>
    IReadOnlyList<Type> InitializePlugins(IServiceCollection services);
}