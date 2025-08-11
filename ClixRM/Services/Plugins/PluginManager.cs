using System.Data;
using System.Reflection;
using System.Text.Json;
using ClixRM.Models;
using ClixRM.Sdk.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ClixRM.Services.Plugins;

public class PluginManager : IPluginManager
{
    private readonly string _pluginsRootPath;
    private readonly string _cacheFilePath;
    private readonly ILogger<PluginManager> _logger;
    
    public PluginManager(ILogger<PluginManager> logger)
    {
        _logger = logger;
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ClixRM");
        
        _pluginsRootPath = Path.Combine(appDataPath, "ClixRM");
        _cacheFilePath = Path.Combine(appDataPath, "plugin-cache.json");
    }

    public IReadOnlyList<Type> InitializePlugins(IServiceCollection services)
    {
        throw new NotImplementedException();
    }

    private void EnsurePluginDirectorExists()
    {
        if (Directory.Exists(_pluginsRootPath)) return;
        
        _logger.LogInformation("Plugins directory not found, creating it at {Path}", _pluginsRootPath);
        Directory.CreateDirectory(_pluginsRootPath);
    }

    private Dictionary<string, DateTime> GetCurrentFileSignatures()
    {
        return Directory.GetFiles(_pluginsRootPath, "*.dll", SearchOption.AllDirectories)
            .ToDictionary(f => f, f => new FileInfo(f).LastWriteTimeUtc);
    }

    private PluginCache? LoadCache()
    {
        if (!File.Exists(_cacheFilePath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(_cacheFilePath);
            return JsonSerializer.Deserialize<PluginCache>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read or deserialize plugin cache file. It may be corrupted, " +
                                   "consider deleting it.");
            return null;
        }
    }

    private bool IsCacheValid(PluginCache? cache, IReadOnlyDictionary<string, DateTime> currentSignatures)
    {
        if (cache == null) return false;

        if (cache.FileSignatures.Count != currentSignatures.Count) return false;
        
        return cache.FileSignatures.All(cachedSig => 
            currentSignatures.TryGetValue(cachedSig.Key, out var currentTimeStamp) &&
            currentTimeStamp == cachedSig.Value);
    }

    private void WriteCache(PluginCache cache)
    {
        try
        {
            var json = JsonSerializer.Serialize(cache, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_cacheFilePath, json);
            _logger.LogInformation("Updated plugin cache at {Path}", _cacheFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write plugin cache file.");
        }
    }

    private List<Type> LoadCommandsFromCache(PluginCache cache, IServiceCollection services)
    {
        var commandTypes = new List<Type>();
        foreach (var typeName in cache.CommandTypeNames)
        {
            var type = Type.GetType(typeName);
            if (type != null)
            {
                commandTypes.Add(type);
            }
            else
            {
                _logger.LogWarning("Could not resolve type '{TypeName} from cache. A full scan may be needed.", typeName);
                throw new TypeLoadException($"Cannot find type '{typeName}' from cache.");
            }
        }
        return commandTypes;
    }

    private void QuickLoadPluginsForServiceConfiguration(PluginCache cache, IServiceCollection services)
    {
        var assemblies = cache.FileSignatures.Keys.Select(Assembly.LoadFrom).Distinct();
        foreach (var assembly in assemblies)
        {
            var pluginType = assembly.GetTypes()
                .FirstOrDefault(t => typeof(IClixrmPlugin).IsAssignableFrom(t) && !t.IsInterface);

            if (pluginType == null) continue;
            
            var pluginInstance = (IClixrmPlugin)Activator.CreateInstance(pluginType)!;
            pluginInstance.ConfigureServices(services);
        }
    }

    private List<Type> ScanAndLoadPlugins(IServiceCollection services)
    {
        var commandTypes = new List<Type>();
        var pluginDirectories = Directory.GetDirectories(_pluginsRootPath);

        foreach (var pluginDir in pluginDirectories)
        {
            try
            {
                var pluginAssembly = LoadPluginAssemblyFromDirectory(pluginDir);
                if (pluginAssembly == null) continue;

                var pluginTypes = pluginAssembly.GetTypes()
                    .Where(t => typeof(IClixrmPlugin).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false });

                foreach (var pluginType in pluginTypes)
                {
                    var pluginInstance = (IClixrmPlugin)Activator.CreateInstance(pluginType)!;
                    pluginInstance.ConfigureServices(services);
                    var commandType = pluginInstance.GetCommandType();
                    commandTypes.Add(commandType);
                    _logger.LogDebug("Discovery command '{CommandName}' from plugin '{PluginName}'.", commandType.Name, pluginType.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin from directory {Directory}", pluginDir);
            }
        }
        
        return commandTypes;
    }

    private Assembly? LoadPluginAssemblyFromDirectory(string pluginDirectory)
    {
        var dllFiles = Directory.GetFiles(pluginDirectory, "*.dll");

        foreach (var dllFile in dllFiles)
        {
            try
            {
                var assembly = Assembly.LoadFrom(dllFile);
                if (assembly.GetTypes().Any(t => typeof(IClixrmPlugin).IsAssignableFrom(t) && !t.IsInterface))
                {
                    return assembly;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load assembly {DllFile}", dllFile);
            }
        }
        
        _logger.LogWarning("No assembly with IClixrmPlugin implemented found in {Directory}.", pluginDirectory);
        return null;
    }
}