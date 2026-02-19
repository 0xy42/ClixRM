using ClixRM.Models;
using ClixRM.Sdk.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClixRM.Formatter;

public class ListUserRolesCommandFormatter : ICommandResultFormatter<List<SecurityRoleCheckResult>>
{
    private readonly IOutputManager _outputManager;

    public ListUserRolesCommandFormatter(IOutputManager outputManager)
    {
        _outputManager = outputManager; 
    }

    public void Format(List<SecurityRoleCheckResult> results)
    {
        if (results.Count == 0)
        {
            _outputManager.PrintWarning("Found no security roles assigned to the user");
            return;
        }

        _outputManager.PrintSuccess($"Found {results.Count} security roles for the user");

        var groupedResults = results.GroupBy(r => r.GrantType).OrderBy(g => g.Key);

        foreach (var group in groupedResults)
        {
            _outputManager.PrintInfo($"\n--- Granted via: {group.Key} ---");
            var orderedGroup = group.OrderBy(r => r.RoleName).ThenBy(r => r.TeamName);

            foreach (var result in orderedGroup)
            {
                if (result.GrantType == "Direct")
                {
                    _outputManager.PrintInfo($"- Role: \"{result.RoleName}\" ({result.RoleId})");
                }
                else
                {
                    _outputManager.PrintInfo(
                        $"- Role: \"{result.RoleName}\" ({result.RoleId}) | Team: \"{result.TeamName}\" ({result.TeamId})"
                    );
                }
            }
        }
    }
}
