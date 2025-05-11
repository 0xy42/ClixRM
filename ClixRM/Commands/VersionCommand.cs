using System;
using System.CommandLine;
using System.Reflection; 
using System.Threading.Tasks; 
using ClixRM.Services.Output;
using Microsoft.Extensions.Logging;

namespace ClixRM.Commands; 

public class VersionCommand : Command
{
    private readonly IOutputManager _outputManager;
    private readonly ILogger<VersionCommand> _logger;

    public VersionCommand(IOutputManager outputManager, ILogger<VersionCommand> logger)
        : base("version", "Display the ClixRM tool version.")
    {
        _outputManager = outputManager ?? throw new ArgumentNullException(nameof(outputManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        this.SetHandler(HandleCommand);
    }

    private void HandleCommand()
    {
        try
        {
            var entryAssembly = Assembly.GetEntryAssembly();

            var versionString = "unknown";
            if (entryAssembly != null)
            {
                var informationalVersion = entryAssembly
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                    .InformationalVersion;

                if (!string.IsNullOrEmpty(informationalVersion))
                {
                    var plusIndex = informationalVersion.IndexOf('+');
                    if (plusIndex > 0)
                    {
                        var version = informationalVersion[..plusIndex];
                        var hash = informationalVersion[(plusIndex + 1)..];
                        if (hash.Length > 7)
                            hash = hash[..7];

                        versionString = $"{version} ({hash})";
                    }
                    else
                    {
                        versionString = informationalVersion;
                    }
                }
                else
                {
                    var assemblyVersion = entryAssembly.GetName().Version;
                    versionString = assemblyVersion != null
                        ? assemblyVersion.ToString()
                        : "unknown";
                }
            }
            else
            {
                versionString = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown (executing)";
                _logger.LogWarning("Could not get entry assembly. Falling back to executing assembly version: {VersionString}", versionString);
            }

            _outputManager.PrintInfo($"ClixRM Version: {versionString}");
            _logger.LogDebug("Displayed ClixRM Version: {VersionString}", versionString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving application version.");
            _outputManager.PrintError($"Error retrieving application version: {ex.Message}");
        }
    }
}