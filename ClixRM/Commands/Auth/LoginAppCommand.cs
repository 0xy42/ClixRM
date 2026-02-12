using ClixRM.Services.Authentication;
using ClixRM.Services.Output;
using System.CommandLine;
using System.Text;
using ClixRM.Sdk.Services;

namespace ClixRM.Commands.Auth;

public class LoginAppCommand : Command
{
    private readonly ISecureStorage _secureStorage;
    private readonly IAuthService _authService;
    private readonly IOutputManager _outputManager;

    public LoginAppCommand(ISecureStorage secureStorage, IAuthService authService, IOutputManager outputManager)
        : base("login-app", "Authenticate and store connection for an environment with an app registration.")
    {
        _secureStorage = secureStorage;
        _authService = authService;
        _outputManager = outputManager;

        var clientIdOption = CreateClientIdOption();
        var urlOption = CreateUrlOption();
        var connectionNameOption = CreateConnectionNameOption();
        var setActiveOption = CreateSetActiveOption();

        AddOption(clientIdOption);
        AddOption(urlOption);
        AddOption(connectionNameOption);
        AddOption(setActiveOption);

        this.SetHandler(HandleLogin, clientIdOption, urlOption, connectionNameOption, setActiveOption);
    }

    private static Option<Guid> CreateClientIdOption()
    {
        return new Option<Guid>(["--client-id", "-c"], "The application client ID for authentication")
        {
            IsRequired = true
        };
    }

    private static Option<string> CreateUrlOption()
    {
        return new Option<string>(["--url", "-u"], "The tenant URL of the environment")
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

    private static Option<bool> CreateSetActiveOption()
    {
        return new Option<bool>(["--set-active", "-a"], "Set the new connection login as active connection.");
    }

    private async Task HandleLogin(Guid clientId, string url, string connectionName, bool doSetActive)
    {

        var clientSecret = PromptForSecret("Enter Client Secret: ");
        

        _outputManager.PrintInfo($"Authenticating to environment '{url}' with client ID '{clientId}'...");

        try
        {
            var connection = await _authService.AuthenticateAppAsync(clientId, clientSecret, url, connectionName);

            _secureStorage.SaveConnection(connection);

            bool wasSetActive = false;
            if (_secureStorage.GetActiveConnectionIdentifier() == null || doSetActive)
            {
                _secureStorage.SetActiveEnvironment(connectionName);
                wasSetActive = true;
            }

            var output = $"Successfully authenticated and stored connection as '{connectionName}'.";
            if (wasSetActive)
            {
                output = output + $" New environment {connectionName} was set as active.";
            }
            _outputManager.PrintSuccess(output);
        }
        catch (Exception ex)
        {
            _outputManager.PrintError($"Authentication failed: {ex.Message}");
        }
    }

    private string PromptForSecret(string promptMessage)
    {
        _outputManager.PrintInfo(promptMessage);

        var secretBuilder = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                break;
            }

            if (key.Key == ConsoleKey.Backspace && secretBuilder.Length > 0)
            {
                secretBuilder.Length--;
            }
            else if (!char.IsControl(key.KeyChar))
            {
                secretBuilder.Append(key.KeyChar);
            }
        }

        return secretBuilder.ToString();
    }
}