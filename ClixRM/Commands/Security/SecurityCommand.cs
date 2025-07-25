﻿using System.CommandLine;

namespace ClixRM.Commands.Security
{
    public class SecurityCommand : Command
    {
        public SecurityCommand(
            PrivilegeCheckCommand privCheckCommand, 
            ListUserRolesCommand secCheckCommand,
            UsersWithRoleCommand userWithRoleCommand)
            : base("sec", "Commands for interaction with security related Dynamics components.")
        {
            AddCommand(privCheckCommand);
            AddCommand(secCheckCommand);
            AddCommand(userWithRoleCommand);
        }
    }
}
