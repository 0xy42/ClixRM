using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClixRM.Sdk.Services;
using ClixRM.Services.Authentication;
using ClixRM.Services.Output;

namespace ClixRM.Commands.Auth
{
    public class LoginUserCommand : Command
    {
        private readonly ISecureStorage _storage;
        private readonly IAuthService _authService;
        private readonly IOutputManager _outputManager;

        public LoginUserCommand(ISecureStorage storage, IAuthService authService, IOutputManager outputManager) : base("login-user", "Authenticate and store connection for an environment with a user login.") 
        {
            _storage = storage;
            _authService = authService;
            _outputManager = outputManager;

            var urlOption = CreateUrlOption();
            var connectionNameOption = CreateConnectionNameOption();
            var setActiveOption = CreateSetActiveOption();

            AddOption(urlOption);
            AddOption(connectionNameOption);
            AddOption(setActiveOption);

            this.SetHandler(HandleUserLogin, urlOption, connectionNameOption, setActiveOption);
        }

        private static Option<string> CreateUrlOption()
        {
            return new Option<string>(["--url", "-u"], "The tenant URL of the environment.")
            {
                IsRequired = true,
            };
        }

        private static Option<string> CreateConnectionNameOption()
        {
            return new Option<string>(["--connection-name", "-n"], "A user-friendly name for the connection.");
        }

        private static Option<bool> CreateSetActiveOption()
        {
            return new Option<bool>(["--set-active", "-a"], "Set the new connection as active connection.");
        }

        private async Task HandleUserLogin(string url, string connectionName, bool doSetActive)
        {
            _outputManager.PrintInfo($"Attempting to log in to environment: {url}");
            _outputManager.PrintInfo("Your web browser will now open for your sign in. Please complete authentication there.");

            try
            {
                var connectionDetails = await _authService.AuthenticateWithUserAsync(url, connectionName);

                _storage.SaveConnection(connectionDetails);

                bool wasSetActive = false;
                if (_storage.GetActiveConnectionIdentifier() == null || doSetActive)
                {
                    _storage.SetActiveEnvironment(connectionName);
                    wasSetActive = true;
                }

                _outputManager.PrintSuccess($"Successfully logged in as {connectionDetails.UserPrincipalName}.");
                _outputManager.PrintSuccess($"Connection {connectionDetails.EnvironmentName} has been saved.");

                if (wasSetActive)
                {
                    _outputManager.PrintSuccess($"Connection {connectionDetails.EnvironmentName} has been set as active environment.");
                }
            }
            catch (OperationCanceledException)
            {
                _outputManager.PrintWarning("Authentication was cancelled by the user. No connection was saved.");
            }
            catch (Exception ex)
            {
                _outputManager.PrintError($"\nAn unexpected error occurred during login: {ex.Message}");
            }
        }
    }
}
