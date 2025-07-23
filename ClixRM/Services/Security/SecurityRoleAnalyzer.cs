using System.CommandLine.Help;
using System.Data;
using ClixRM.Models;
using ClixRM.Services.Authentication;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace ClixRM.Services.Security;

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

        var privilegeId = await GetPrivilegeId(serviceClient, privilegeName);
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
        
        var directRoles = await GetDirectSecurityRolesAsync(serviceClient, userId);

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

        var teams = await GetUserTeamsAsync(serviceClient, userId);
        foreach (var team in teams.Entities)
        {
            var teamName = team.GetAttributeValue<AliasedValue>("team.name")?.Value?.ToString() ?? "Unknown Team";
            var teamId = team.GetAttributeValue<Guid>("teamid");
            var teamRoles = await GetTeamSecurityRolesAsync(serviceClient, teamId);

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

    public async Task<List<UserWithRoleResult>> GetUsersWithRoleAsync(string roleName)
    {
        var serviceClient = await dataverseConnector.GetServiceClientAsync();
        var roleId = await GetRoleIdByNameAsync(serviceClient, roleName);

        if (roleId.Equals(Guid.Empty))
        {
            throw new Exception($"Security role with name '{roleName}' does not exist.");
        }

        return await GetUsersWithRoleAsync(roleId);
    }


    public async Task<List<UserWithRoleResult>> GetUsersWithRoleAsync(Guid roleId)
    {
        var serviceClient = await dataverseConnector.GetServiceClientAsync();
        var results = new List<UserWithRoleResult>();

        var directUsers = await GetUsersWithDirectRoleAsync(serviceClient, roleId);

        foreach (var directUser in directUsers.Entities)
        {
            var userId = directUser.GetAttributeValue<Guid>("systemuserid");
            var userName = directUser.GetAttributeValue<AliasedValue>("user.fullname")?.Value?.ToString() ?? "Unkown User";

            results.Add(new UserWithRoleResult("Direct", userId, userName, null, null));
        }

        var teamsWithRole = await GetTeamsWithRoleAsync(serviceClient, roleId);

        foreach (var team in teamsWithRole.Entities)
        {
            var teamId = team.GetAttributeValue<Guid>("teamid");
            var teamName = team.GetAttributeValue<AliasedValue>("team.name")?.Value?.ToString() ?? "Unkown Team";

            var usersInTeam = await GetUsersInTeamAsync(serviceClient, teamId);
            foreach (var user in usersInTeam.Entities)
            {
                var userId = user.GetAttributeValue<Guid>("systemuserid");
                var userName = user.GetAttributeValue<AliasedValue>("user.fullname")?.Value?.ToString() ?? "Unkown User";

                results.Add(new UserWithRoleResult("Team", userId, userName, teamName, teamId));
            }
        }

        return results;
    }

    private async Task<Guid> GetPrivilegeId(IOrganizationServiceAsync2 serviceClient, string privilegeName)
    {
        var query = new QueryExpression("privilege")
        {
            ColumnSet = new ColumnSet("privilegeid"),
            Criteria = { Conditions = { new ConditionExpression("name", ConditionOperator.Equal, privilegeName) } }
        };

        var result = await serviceClient.RetrieveMultipleAsync(query);
        return result.Entities.Count > 0 ? result.Entities[0].GetAttributeValue<Guid>("privilegeid") : Guid.Empty;
    }

    /// <summary>
    ///     Checks roles assigned directly to the user.
    /// </summary>
    /// <returns>A list of PrivilegeCheckResult for directly granted privileges.</returns>
    private async Task<List<PrivilegeCheckResult>> CheckDirectSecurityRoles(IOrganizationServiceAsync2 serviceClient, Guid userId, Guid privilegeId, string privilegeName)
    {
        var directRoles = await GetDirectSecurityRolesAsync(serviceClient, userId);
        var directResults = new List<PrivilegeCheckResult>();

        foreach (var role in directRoles.Entities)
        {
            var roleId = role.GetAttributeValue<Guid>("roleid");
            var roleName = role.GetAttributeValue<AliasedValue>("role.name")?.Value?.ToString() ?? "Unknown Role";

            var privilegeScope = await GetRolePrivilegeLevelAsync(serviceClient, roleId, privilegeId);
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
    private async Task<List<PrivilegeCheckResult>> CheckTeamSecurityRoles(IOrganizationServiceAsync2 serviceClient, Guid userId, Guid privilegeId, string privilegeName) // Added privilegeName param
    {
        var teams = await GetUserTeamsAsync(serviceClient, userId);
        var teamResults = new List<PrivilegeCheckResult>();

        foreach (var team in teams.Entities)
        {
            var teamName = team.GetAttributeValue<AliasedValue>("team.name")?.Value?.ToString() ?? "Unknown Team";
            var teamId = team.GetAttributeValue<Guid>("teamid");
            var teamRoles = await GetTeamSecurityRolesAsync(serviceClient, teamId);

            foreach (var role in teamRoles.Entities)
            {
                var roleName = role.GetAttributeValue<AliasedValue>("role.name")?.Value?.ToString() ?? "Unknown Role";
                var roleId = role.GetAttributeValue<Guid>("roleid");

                var privilegeScope = await GetRolePrivilegeLevelAsync(serviceClient, roleId, privilegeId);
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

    private async Task<EntityCollection> GetDirectSecurityRolesAsync(IOrganizationServiceAsync2 serviceClient, Guid userId)
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

        return await serviceClient.RetrieveMultipleAsync(query);
    }

    private async Task<EntityCollection> GetUserTeamsAsync(IOrganizationServiceAsync2 serviceClient, Guid userId)
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

        return await serviceClient.RetrieveMultipleAsync(query);
    }

    private async Task<EntityCollection> GetTeamSecurityRolesAsync(IOrganizationServiceAsync2 serviceClient, Guid teamId)
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

        return await serviceClient.RetrieveMultipleAsync(query);
    }

    private async Task<string?> GetRolePrivilegeLevelAsync(IOrganizationServiceAsync2 serviceClient, Guid roleId, Guid privilegeId)
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

        var result = await serviceClient.RetrieveMultipleAsync(query);

        if (!result.Entities.Any()) return null;

        if (!result.Entities[0].Contains("privilegedepthmask")) return null;

        var privilegeDepthMask = result.Entities[0].GetAttributeValue<int>("privilegedepthmask");

        return GetPrivilegeLevelDescription(privilegeDepthMask);
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

    private async Task<EntityCollection> GetUsersWithDirectRoleAsync(IOrganizationServiceAsync2 serviceClient, Guid roleId)
    {
        var query = new QueryExpression("systemuserroles")
        {
            ColumnSet = new ColumnSet("systemuserid"),
            Criteria =
            {
                Conditions =
                {
                    new ConditionExpression("roleid", ConditionOperator.Equal, roleId)
                }
            }
        };

        query.LinkEntities.Add(new LinkEntity("systemuserroles", "systemuser", "systemuserid", "systemuserid", JoinOperator.Inner)
        {
            Columns = new ColumnSet("fullname"),
            EntityAlias = "user"
        });

        return await serviceClient.RetrieveMultipleAsync(query);
    }

    private async Task<EntityCollection> GetTeamsWithRoleAsync(IOrganizationServiceAsync2 serviceClient, Guid roleId)
    {
        var query = new QueryExpression("teamroles")
        {
            ColumnSet = new ColumnSet("teamid"),
            Criteria =
            {
                Conditions =
                {
                    new ConditionExpression("roleid", ConditionOperator.Equal, roleId)
                }
            }
        };

        query.LinkEntities.Add(new LinkEntity("teamroles", "team", "teamid", "teamid", JoinOperator.Inner)
        {
            Columns = new ColumnSet("name"),
            EntityAlias = "team"
        });

        return await serviceClient.RetrieveMultipleAsync(query);
    }

    private async Task<EntityCollection> GetUsersInTeamAsync(IOrganizationServiceAsync2 serviceClient, Guid teamId)
    {
        var query = new QueryExpression("teammembership")
        {
            ColumnSet = new ColumnSet("systemuserid"),
            Criteria =
            {
                Conditions =
                {
                    new ConditionExpression("teamid", ConditionOperator.Equal, teamId)
                }
            }
        };

        query.LinkEntities.Add(new LinkEntity("teammembership", "systemuser", "systemuserid", "systemuserid", JoinOperator.Inner)
        {
            Columns = new ColumnSet("fullname"),
            EntityAlias = "user"
        });

        return await serviceClient.RetrieveMultipleAsync(query);
    }

    private async Task<Guid> GetRoleIdByNameAsync(IOrganizationServiceAsync2 service, string roleName)
    {
        var query = new QueryExpression("role")
        {
            ColumnSet = new ColumnSet("roleid", "name"),
            Criteria =
            {
                Conditions =
                {
                    new ConditionExpression("name", ConditionOperator.Equal, roleName)
                }
            }
        };

        var result = await service.RetrieveMultipleAsync(query);

        return result.Entities.FirstOrDefault()?.Id ?? Guid.Empty;
    }
}