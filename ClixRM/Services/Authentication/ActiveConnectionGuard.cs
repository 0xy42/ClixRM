using ClixRM.Sdk.Services;

namespace ClixRM.Services.Authentication;

/// <summary>
///     Only exposes safe methods for plugins.
///     Wraps real ISecureStorage.
/// </summary>
public class ActiveConnectionGuard : IActiveConnectionGuard
{
    private readonly ISecureStorage _secureStorage;
    
    public ActiveConnectionGuard(ISecureStorage secureStorage)
    {
        _secureStorage = secureStorage;
    }

    public bool DoesActiveConnectionExist() => _secureStorage.DoesActiveConnectionExist();
}