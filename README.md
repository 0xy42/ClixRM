# ClixRM

ClixRM is a command-line tool designed to streamline and simplify administrative interactions with Microsoft Dataverse environments. It provides developers and administrators with an efficient way to manage connections, authenticate, and perform operations on Dataverse instances directly from the command line.

> **Note:** ClixRM is currently in early, active development. Features and functionality are subject to change frequently, and breaking changes may occur with updates. Use it cautiously, especially in production environments.

---

## Features

ClixRM offers the following features:

### Authentication
- **Login Command (`login`)**  
  Authenticate to a Dataverse environment using client credentials and store the connection securely for future use.  
  Options:
  - `--client-id` (`-c`): The application client ID for authentication (required).
  - `--client-secret` (`-s`): The client secret for authentication (required).
  - `--tenant-id` (`-t`): The tenant ID of the environment (required).
  - `--url` (`-a`): The tenant URL of the environment (required).
  - `--connection-name` (`-n`): A user-friendly name for the connection (required).

- **Switch Environment Command (`switch`)**  
  Switch to a different environment.  
  Arguments:
  - `<environment>`: The environment to switch to (required).

### Security
- **Privilege Check Command (`priv-check`)**  
  Check a user's privilege for a specific entity or action in the Dataverse environment.  
  Options:
  - `--user-id`: The ID of the user to check privileges for (must be a valid GUID, required).
  - `--privilege`: The logical name of the privilege to check (e.g., `prvCreateAccount`, required).

### Flow Management
- **Field Dependency Analysis (`flow-field-dependency`)**  
  Analyze Power Automate flows to find dependencies on specific entity fields.  
  Options:
  - `--entity`: Logical name of the entity (singular preferred, required).
  - `--column`: Logical name of the column to analyze (required).
  - `--action`: Filter to include only actions of a specific type (optional).
  - `--actions-only`: If set, only actions will be included (optional).
  - `--triggers-only`: If set, only triggers will be included (optional).
  - `--online-solution`: Unique name of the solution to download from the online environment (optional).
  - `--dir`: Path to the unzipped solution directory containing cloud flows (optional).
  - `--force-download`: Force download of the solution, ignoring any cached version (optional).

- **Entity Event Trigger Analysis (`flow-entity-event`)**  
  Analyze Power Automate flows to find triggers based on specific entity events.  
  Options:
  - `--solution-directory`: Path to the unzipped solution directory (required).
  - `--entity-name`: Logical name of the entity (singular preferred, required).
  - `--event-name`: The name of the event (e.g., "Update", "Create", "Delete", required).

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