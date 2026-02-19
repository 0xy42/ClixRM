using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClixRM.Models;
using ClixRM.Sdk.Commands;
using ClixRM.Sdk.Services;
using ClixRM.Services.Output;
using ClixRM.Services.Security;

namespace ClixRM.Commands.Security
{
    public class ListUserRolesCommand : CrmConnectedCommand<List<SecurityRoleCheckResult>>
    {
        private readonly IOutputManager _outputManager;
        private readonly ISecurityRoleAnalyzer _securityRoleAnalyzer;

        public ListUserRolesCommand(
            IOutputManager outputManager, 
            ISecurityRoleAnalyzer securityRoleAnalyzer, 
            IActiveConnectionGuard activeConnectionGuard, 
            ICommandResultFormatter<List<SecurityRoleCheckResult>> formatter)
            : base("list-user-roles", "List all security roles assigned to a specific user (directly or via teams).", 
                  activeConnectionGuard, formatter)
        {
            _outputManager = outputManager;
            _securityRoleAnalyzer = securityRoleAnalyzer;

            var userIdOption = CreateUserIdOption();

            AddOption(userIdOption);
           
            this.SetHandler(HandleCommandAsync, userIdOption);
        }

        private static Option<string> CreateUserIdOption()
        {
            var userIdOption = new Option<string>(
                aliases: ["--user-id", "-u"],
                description: "The GUID of the user to check security roles for.")
            {
                IsRequired = true,
                ArgumentHelpName = "guid"
            };
            userIdOption.AddValidator(ValidateGuid);

            return userIdOption;
        }

        private static void ValidateGuid(OptionResult optionResult)
        {
            var value = optionResult.GetValueOrDefault<string>();
            if (!Guid.TryParse(value, out _))
            {
                optionResult.ErrorMessage = "The --user-id must be a valid GUID.";
            }
        }

        private async Task HandleCommandAsync(string userIdstring)
        {
            var userId = Guid.Parse(userIdstring);

            _outputManager.PrintInfo($"Checking security roles for user '{userId}'...");

            try
            {
                var results = await _securityRoleAnalyzer.CheckSecurityRolesAsync(userId);

                Formatter.Format(results);
            }
            catch (Exception ex)
            {
                _outputManager.PrintError($"An error occured during security role check: {ex.Message}");
            }
        }
    }
}
