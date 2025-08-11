namespace ClixRM.Models;

/// <summary>
///     Represents the cached data for discovered plugins to speed up startup time.
/// </summary>
public class PluginCache
{
    /// <summary>
    ///     Default constructor.
    /// </summary>
    /// <param name="fileSignatures">The plugin file signatures.</param>
    /// <param name="commandTypeNames">The found command type names.</param>
    public PluginCache(Dictionary<string, DateTime> fileSignatures, List<string> commandTypeNames)
    {
        FileSignatures = fileSignatures;
        CommandTypeNames = commandTypeNames;
    }
    
    /// <summary>
    /// A dictionary mapping plugin assembly file paths to their last write time. Used to detect if plugin cache is stale.
    /// </summary>
    public Dictionary<string, DateTime> FileSignatures { get; set; }
    
    /// <summary>
    /// The assembly-qualified names of the discovered plugin command types.
    /// </summary>
    public List<string> CommandTypeNames { get; set; }
}