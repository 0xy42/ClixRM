using ClixRM.Services.Authentication;
using System.CommandLine;

namespace ClixRM.Commands.Auth;

public class LoginCommand : Command
{
    private readonly ISecureStorage _secureStorage;
    private readonly IAuthService _authService;

    public LoginCommand(ISecureStorage secureStorage, IAuthService authService)
        : base("login", "Authenticate and store connection for an environment")
    {
        _secureStorage = secureStorage;
        _authService = authService;

        var clientIdOption = CreateClientIdOption();
        var clientSecretOption = CreateClientSecretOption();
        var tenantIdOption = CreateTenantIdOption();
        var urlOption = CreateUrlOption();
        var connectionNameOption = CreateConnectionNameOption();

        AddOption(clientIdOption);
        AddOption(clientSecretOption);
        AddOption(tenantIdOption);
        AddOption(urlOption);
        AddOption(connectionNameOption);

        this.SetHandler(HandleLogin, clientIdOption, clientSecretOption, tenantIdOption, urlOption, connectionNameOption);
    }

    private static Option<string> CreateClientIdOption()
    {
        return new Option<string>(["--client-id", "-c"], "The application client ID for authentication")
        {
            IsRequired = true
        };
    }

    private static Option<string> CreateClientSecretOption()
    {
        return new Option<string>(["--client-secret", "-s"], "The client secret for authentication")
        {
            IsRequired = true
        };
    }

    private static Option<string> CreateTenantIdOption()
    {
        return new Option<string>(["--tenant-id", "-t"], "The tenant ID of the environment")
        {
            IsRequired = true
        };
    }

    private static Option<string> CreateUrlOption()
    {
        return new Option<string>(["--url", "-a"], "The tenant URL of the environment")
        {
            IsRequired = true
        };
    }

    private static Option<string> CreateConnectionNameOption()
    {
        return new Option<string>(["--connection-name", "-n"], "A user-friendly name for the connection")
        {
            IsRequired = true
        };
    }

    private async Task HandleLogin(string clientId, string clientSecret, string tenantId, string url, string connectionName)
    {
        Console.WriteLine($"Authenticating to tenant '{tenantId}' with client ID '{clientId}'...");

        try
        {
            var connection = await _authService.AuthenticateAsync(clientId, clientSecret, tenantId, url, connectionName);

            _secureStorage.SaveConnection(connection);

            Console.WriteLine($"Successfully authenticated and stored connection as '{connectionName}'.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Authentication failed: {ex.Message}");
        }
    }
}