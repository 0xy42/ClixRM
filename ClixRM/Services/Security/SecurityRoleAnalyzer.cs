using System.Data;
using ClixRM.Services.Authentication;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace ClixRM.Services.Security;

public record PrivilegeCheckResult(
    string GrantType,
    string RoleName,
    Guid RoleId,
    string PrivilegeName,
    string PrivilegeScope,
    string? TeamName,
    Guid? TeamId
);

public record SecurityRoleCheckResult(
    string GrantType,
    string RoleName, 
    Guid RoleId,
    string? TeamName,
    Guid? TeamId
);

public class SecurityRoleAnalyzer(IDataverseConnector dataverseConnector) : ISecurityRoleAnalyzer
{
    /// <summary>
    ///     Checks how a specific privilege is granted to a user, either directly or via teams.
    /// </summary>
    /// <param name="userId">The GUID of the user to check.</param>
    /// <param name="privilegeName">The name of the privilege (e.g., "prvReadAccount").</param>
    /// <returns>A Task containing a list of PrivilegeCheckResult objects detailing how the privilege is granted. Returns an empty list if the privilege is not found in the system or not granted to the user.</returns>
    public async Task<List<PrivilegeCheckResult>> CheckPrivilegeAsync(Guid userId, string privilegeName)
    {
        var serviceClient = await dataverseConnector.GetServiceClientAsync();
        var results = new List<PrivilegeCheckResult>();

        var privilegeId = await Task.Run(() => GetPrivilegeId(serviceClient, privilegeName));
        if (privilegeId.Equals(Guid.Empty))
        {
            throw new Exception("Privilege not found in the system.");
        }

        var directResults = await Task.Run(() => CheckDirectSecurityRoles(serviceClient, userId, privilegeId, privilegeName));
        results.AddRange(directResults);

        var teamResults = await Task.Run(() => CheckTeamSecurityRoles(serviceClient, userId, privilegeId, privilegeName));
        results.AddRange(teamResults);

        return results;
    }

    public async Task<List<SecurityRoleCheckResult>> CheckSecurityRolesAsync(Guid userId)
    {
        var serviceClient = await dataverseConnector.GetServiceClientAsync();
        var results = new List<SecurityRoleCheckResult>();
        
        var directRoles = GetDirectSecurityRoles(serviceClient, userId);

        foreach ( var role in directRoles.Entities )
        {
            var roleId = role.GetAttributeValue<Guid>("roleid");
            var roleName = role.GetAttributeValue<AliasedValue>("role.name")?.Value?.ToString() ?? "Unknown Role";

            results.Add(
                new SecurityRoleCheckResult(
                    GrantType: "Direct",
                    RoleName: roleName,
                    RoleId: roleId,
                    TeamName: null,
                    TeamId: null
                )
            );
        } 

        var teams = GetUserTeams(serviceClient, userId);
        foreach (var team in teams.Entities)
        {
            var teamName = team.GetAttributeValue<AliasedValue>("team.name")?.Value?.ToString() ?? "Unknown Team";
            var teamId = team.GetAttributeValue<Guid>("teamid");
            var teamRoles = GetTeamSecurityRoles(serviceClient, teamId);

            foreach (var teamRole in teamRoles.Entities)
            {
                var roleName = teamRole.GetAttributeValue<AliasedValue>("role.name")?.Value?.ToString() ?? "Unknown Role";
                var roleId = teamRole.GetAttributeValue<Guid>("roleid");

                results.Add(
                    new SecurityRoleCheckResult(
                        GrantType: "Team",
                        RoleName: roleName,
                        RoleId: roleId,
                        TeamName: teamName,
                        TeamId: teamId
                    )
                );
            }
        }

        return results; 
    }

    private Guid GetPrivilegeId(IOrganizationService serviceClient, string privilegeName)
    {
        var query = new QueryExpression("privilege")
        {
            ColumnSet = new ColumnSet("privilegeid"),
            Criteria = { Conditions = { new ConditionExpression("name", ConditionOperator.Equal, privilegeName) } }
        };

        var result = serviceClient.RetrieveMultiple(query);
        return result.Entities.Count > 0 ? result.Entities[0].GetAttributeValue<Guid>("privilegeid") : Guid.Empty;
    }

    /// <summary>
    ///     Checks roles assigned directly to the user.
    /// </summary>
    /// <returns>A list of PrivilegeCheckResult for directly granted privileges.</returns>
    private List<PrivilegeCheckResult> CheckDirectSecurityRoles(IOrganizationService serviceClient, Guid userId, Guid privilegeId, string privilegeName)
    {
        var directRoles = GetDirectSecurityRoles(serviceClient, userId);
        var directResults = new List<PrivilegeCheckResult>();

        foreach (var role in directRoles.Entities)
        {
            var roleId = role.GetAttributeValue<Guid>("roleid");
            var roleName = role.GetAttributeValue<AliasedValue>("role.name")?.Value?.ToString() ?? "Unknown Role";

            var privilegeScope = GetRolePrivilegeLevel(serviceClient, roleId, privilegeId);
            if (privilegeScope != null)
            {
                directResults.Add(new PrivilegeCheckResult(
                    GrantType: "Direct",
                    RoleName: roleName,
                    RoleId: roleId,
                    PrivilegeName: privilegeName,
                    PrivilegeScope: privilegeScope,
                    TeamName: null,
                    TeamId: null
                ));
            }
        }
        return directResults;
    }

    /// <summary>
    ///     Checks roles assigned via teams the user is a member of.
    /// </summary>
    /// <returns>A list of PrivilegeCheckResult for team-granted privileges.</returns>
    private List<PrivilegeCheckResult> CheckTeamSecurityRoles(IOrganizationService serviceClient, Guid userId, Guid privilegeId, string privilegeName) // Added privilegeName param
    {
        var teams = GetUserTeams(serviceClient, userId);
        var teamResults = new List<PrivilegeCheckResult>();

        foreach (var team in teams.Entities)
        {
            var teamName = team.GetAttributeValue<AliasedValue>("team.name")?.Value?.ToString() ?? "Unknown Team";
            var teamId = team.GetAttributeValue<Guid>("teamid");
            var teamRoles = GetTeamSecurityRoles(serviceClient, teamId);

            foreach (var role in teamRoles.Entities)
            {
                var roleName = role.GetAttributeValue<AliasedValue>("role.name")?.Value?.ToString() ?? "Unknown Role";
                var roleId = role.GetAttributeValue<Guid>("roleid");

                var privilegeScope = GetRolePrivilegeLevel(serviceClient, roleId, privilegeId);
                if (privilegeScope != null)
                {
                    teamResults.Add(new PrivilegeCheckResult(
                        GrantType: "Team",
                        RoleName: roleName,
                        RoleId: roleId,
                        PrivilegeName: privilegeName,
                        PrivilegeScope: privilegeScope,
                        TeamName: teamName,
                        TeamId: teamId
                    ));
                }
            }
        }
        return teamResults;
    }

    private EntityCollection GetDirectSecurityRoles(IOrganizationService serviceClient, Guid userId)
    {
        var query = new QueryExpression("systemuserroles")
        {
            ColumnSet = new ColumnSet("roleid"),
            Criteria = { Conditions = { new ConditionExpression("systemuserid", ConditionOperator.Equal, userId) } }
        };
        query.LinkEntities.Add(new LinkEntity("systemuserroles", "role", "roleid", "roleid", JoinOperator.Inner)
        {
            Columns = new ColumnSet("name"),
            EntityAlias = "role"
        });
        return serviceClient.RetrieveMultiple(query);
    }

    private EntityCollection GetUserTeams(IOrganizationService serviceClient, Guid userId)
    {
        var query = new QueryExpression("teammembership")
        {
            ColumnSet = new ColumnSet("teamid"),
            Criteria = { Conditions = { new ConditionExpression("systemuserid", ConditionOperator.Equal, userId) } }
        };
        query.LinkEntities.Add(new LinkEntity("teammembership", "team", "teamid", "teamid", JoinOperator.Inner)
        {
            Columns = new ColumnSet("name"),
            EntityAlias = "team"
        });
        return serviceClient.RetrieveMultiple(query);
    }

    private EntityCollection GetTeamSecurityRoles(IOrganizationService serviceClient, Guid teamId)
    {
        var query = new QueryExpression("teamroles")
        {
            ColumnSet = new ColumnSet("roleid"),
            Criteria = { Conditions = { new ConditionExpression("teamid", ConditionOperator.Equal, teamId) } }
        };
        query.LinkEntities.Add(new LinkEntity("teamroles", "role", "roleid", "roleid", JoinOperator.Inner)
        {
            Columns = new ColumnSet("name"),
            EntityAlias = "role"
        });
        return serviceClient.RetrieveMultiple(query);
    }

    private string GetPrivilegeLevelDescription(int privilegeDepthMask)
    {
        return privilegeDepthMask switch
        {
            1 => "User",
            2 => "Business Unit",
            4 => "Parent: Child Business Units",
            8 => "Organization",
            _ => "None"
        };
    }

    private string? GetRolePrivilegeLevel(IOrganizationService serviceClient, Guid roleId, Guid privilegeId)
    {
        var query = new QueryExpression("roleprivileges")
        {
            ColumnSet = new ColumnSet("privilegedepthmask"),
            Criteria =
            {
                FilterOperator = LogicalOperator.And,
                Conditions =
                {
                    new ConditionExpression("roleid", ConditionOperator.Equal, roleId),
                    new ConditionExpression("privilegeid", ConditionOperator.Equal, privilegeId)
                }
            }
        };

        var result = serviceClient.RetrieveMultiple(query);

        if (!result.Entities.Any()) return null;

        if (!result.Entities[0].Contains("privilegedepthmask")) return null;

        var privilegeDepthMask = result.Entities[0].GetAttributeValue<int>("privilegedepthmask");
        return GetPrivilegeLevelDescription(privilegeDepthMask);
    }
}