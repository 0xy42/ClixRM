namespace ClixRM.Sdk.Services;

/// <summary>
/// Provides safe checks about active connection state for ClixRM plugins.
/// </summary>
public interface IActiveConnectionGuard
{
    /// <summary>
    /// Checks if a valid, active connection exists.
    /// </summary>
    /// <returns>True if an active connection exists; otherwise, false.</returns>
    bool DoesActiveConnectionExist();
}