# ClixRM

ClixRM is a command-line tool designed to streamline and simplify administrative interactions with Microsoft Dataverse environments. It provides developers and administrators with an efficient way to manage connections, authenticate, and perform operations on Dataverse instances directly from the command line.

> **Note:** ClixRM is currently in early, active development. Features and functionality are subject to change frequently, and breaking changes may occur with updates. Use it cautiously, especially in production environments.

---

## Features

ClixRM offers the following features:

### Authentication
- **Clear Command (`clear`)**
  Clear and remove all existing connections stored on your machine.

- **List Command (`list`)**
  List all existing and stored connections.

- **Login-App Command (`login-app`)**  
  Authenticate to a Dataverse environment using client credentials and store the connection securely for future use.  
  Options:
  - `--client-id` (`-c`): The application client ID for authentication (required).
  - `--client-secret` (`-s`): The client secret for authentication (required).
  - `--url` (`-u`): The tenant URL of the environment (required).
  - `--connection-name` (`-n`): A user-friendly name for the connection (required).
  - `--set-active` (`-a`): Set the newly created connection as active connection.

- **Login-User Command (`login-user`)**
  Authenticate to a Dataverse environment using an interactive user login and store the connection securely for future use. Currently, ClixRM is not a validated Application in Microsoft Partner Program, thus your Tenants admin consent is needed when using ClixRM for interactive user logins.  
  Options: 
  - `--url` (`-u`): The tenant URL of the environment (required).
  - `--connection-name` (`-n`): A user-friendly name for the connection (required).
  - `--set-active` (`-a`): Set the newly created connection as active connection.

- **Show Active Command (`show-active`)**
  Show the currently active used connection.

- **Switch Environment Command (`switch`)**  
  Switch to a different environment.  
  Arguments:
  - `<environmentName>`: The environment to switch to (required).

### Security
- **List security roles Command (`list-security-roles`)**
  Check if a user has a specific security role assigned (directly or via teams).  
  Options: 
  - `--user-id` (`-u`): The GUID of the user to check security roles for. (required)

- **Privilege Check Command (`privilege-check`)**  
  Check a user's privilege for a specific entity or action in the Dataverse environment.  
  Options:
  - `--user-id` (`-u`): The ID of the user to check privileges for (must be a valid GUID, required).
  - `--privilege`: The logical name of the privilege to check (e.g., `prvCreateAccount`, required).

### Flow Management
Currently, all 'Flow' commands feature the following options since they are solution aware commands:
- `--online-solution` (`-s`): The unique name of the solution to download from the online environment.
- `--dir` (`-d`): The path to an unzipped, already downloaded solution.
- `--force-download` (`-f`): Force downloading a new version of the solution instead of using the cache, e.g. in case of updates. 

- **Field Dependency Analysis (`flow-field-dependency`)**  
  Analyze Power Automate flows to find dependencies on specific entity fields.  
  Options:
  - `--entity` (`-e`): Logical name of the entity (singular preferred, required).
  - `--column` (`-c`): Logical name of the column to analyze (required).
  - `--action` (`-a`): Filter to include only actions of a specific type (optional).
  - `--actions-only` (`-ao`): If set, only actions will be included (optional).
  - `--triggers-only` (`-to`): If set, only triggers will be included (optional).

- **Triggered By Entity Message Command (`triggered-by-message`)**  
  Analyze Power Automate flows to find triggers based on specific entity events.  
  Options:
  - `--solution-directory`: Path to the unzipped solution directory (required).
  - `--entity` (`-e`): Logical name of the entity to check (required).
  - `--message` (`-m`): The name of the event message (e.g., "Update", "Create", "Delete", required).

- **Triggers Message Command (`triggers-message`)**
  Check all flows in a solution for triggering specific entity messages (e.g. create account).  
  Options: 
  - `--entity` (`-e`): The logical singular name of the entity targeted by the action.
  - `--message` (`-m`): THe type of operation to search for actions performing. Use '-h' for a list of allowed operations.

---

## Development Status

ClixRM is in **early development**. The following points are important to note:
- Features are actively being added and refined.
- Breaking changes may occur with updates.
- Some commands and features are placeholders and may not yet be fully implemented.

---

## Roadmap

Following are planned to be implemented: 
- `column-security-audit`: Lists all column security profiles associated with a field and the users/teams that have access to read/write it.
- `record-visibility-check`: Checks if a specific user has access to a record and why (or why not), including detailed analysis of roles, privileges, and field-level security.
- `orphaned-record-finder`: Finds records in an entity that are not associated with any parent records via a given relationship.
- `flow-env-compare`: Compares Power Automate flows between two environments.
- `flow-owner-check`: Displays the owner(s) of a flow and their permissions. Identifies flows without an active owner (e.g., owners who left the organization).
- `flow-usage-report`: Generates a report of all flows that interact with a specific entity. Categorizes flows by triggers, actions, and conditions.
- `flow-connection-health-check`: Analyzes all flows in an environment and reports flows with expired/invalid connections or connection references.
- `solution-layer-inspect`: Displays the stack of solution layers (unmanaged, various managed solutions) applied to a specific component.
- `form-field-usage`: Shows which fields are used across all forms of an entity and which aren't, helping identify candidates for cleanup.
- `script-registration-analyzer`: Analyzes events and handlers registered on a form to identify difficult-to-find handlers (e.g., on-change handlers on hidden fields).
- `env-var-manager`: Manage and compare environment variables between two Dynamics 365 instances.
- `merge-publisher-solutions`: Merge multiple unmanaged solutions from a specific publisher into one single solution.
- `column-process-analyzer`: Searches for all workflows, cloud flows, scripts, plugins, etc., that interact with a specific column.

---

## Contributing

Contributions are welcome! If you encounter issues or have feature requests, please open an issue or submit a pull request.

---

## License

This project is licensed under the MIT License. See the license file for details.