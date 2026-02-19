using ClixRM.Models;
using ClixRM.Sdk.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClixRM.Formatter;

public class PrivilegeCheckCommandFormatter : ICommandResultFormatter<List<PrivilegeCheckResult>>
{
    private readonly IOutputManager _outputManager;

    public PrivilegeCheckCommandFormatter(IOutputManager outputManager)
    {
        _outputManager = outputManager;
    }

    public void Format(List<PrivilegeCheckResult> results)
    {
        if (results.Count == 0)
        {
            _outputManager.PrintWarning("Privilege is either not found in the system or not granted to user directly or via teams.");
            return;
        }

        _outputManager.PrintSuccess($"Found {results.Count} grant path(s) for the privilege:");

        var groupedResults = results.GroupBy(r => r.GrantType).OrderBy(g => g.Key);

        foreach (var group in groupedResults)
        {
            _outputManager.PrintInfo($"\n--- Granted via: {group.Key} ---");
            var orderedGroup = group.OrderBy(r => r.RoleName).ThenBy(r => r.TeamName);

            foreach (var result in orderedGroup)
            {
                if (result.GrantType == "Direct")
                {
                    _outputManager.PrintInfo($"- Role: \"{result.RoleName}\" ({result.RoleId}) | Scope: {result.PrivilegeScope}");
                }
                else
                {
                    _outputManager.PrintInfo(
                        $"- Role: \"{result.RoleName}\" ({result.RoleId}) | Team: \"{result.TeamName}\" ({result.TeamId}) | Scope: {result.PrivilegeScope}"
                    );
                }
            }
        }
    }
}
