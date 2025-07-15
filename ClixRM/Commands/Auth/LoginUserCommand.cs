using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        }
    }
}
