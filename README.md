# ClixRM

ClixRM is a command-line tool designed to streamline and simplify administrative interactions with Microsoft Dataverse environments. It provides developers and administrators with an efficient way to manage connections, authenticate, and perform operations on Dataverse instances directly from the command line.

> **Note:** ClixRM is currently in early, active development. Features and functionality are subject to change frequently, and breaking changes may occur with updates. Use it cautiously, especially in production environments.

---

## Commands

ClixRM is organized into top-level command groups. Each group contains subcommands with their own options/arguments.

### `auth` — Authentication commands for managing connections to environments

#### `auth login-app` — Authenticate and store connection for an environment with an app registration.

Options:
- `--client-id`, `-c` *(required)*: The application client ID for authentication.
- `--url`, `-u` *(required)*: The tenant URL of the environment.
- `--connection-name`, `-n` *(required)*: A user-friendly name for the connection.
- `--set-active`, `-a` *(optional, boolean; default: false)*: Set the new connection login as active connection.

Notes:
- The client secret is **prompted interactively** (it is not provided via a CLI option).

---

#### `auth login-user` — Authenticate and store connection for an environment with a user login.

Options:
- `--url`, `-u` *(required)*: The tenant URL of the environment.
- `--connection-name`, `-n` *(optional)*: A user-friendly name for the connection.
- `--set-active`, `-a` *(optional, boolean; default: false)*: Set the new connection as active connection.

---

#### `auth switch` — Switch to a different environment

Arguments:
- `environment`: The environment to switch to.

Notes:
- The command lowercases the environment name before looking it up / setting active.

---

#### `auth list` — List the existing connections.

Options:
- *(none)*

---

#### `auth show-active` — Show the currently active connection.

Options:
- *(none)*

---

#### `auth clear` — Clear and remove all stored connections.

Options:
- *(none)*

---

### `sec` — Commands for interaction with security related Dynamics components.

#### `sec privilege-check` — Check how a specific privilege is granted to a user (directly or via teams).

Options:
- `--user-id`, `-u` *(required)*: The GUID of the user to check privileges for.  
  - Validation: must be a valid GUID.
- `--privilege`, `-p` *(required)*: The logical name of the privilege to check (e.g., `prvCreateAccount`).

---

#### `sec users-with-role` — List all users of an environment that are assigned a specific security role (directly or via teams).

Options (mutually exclusive; **exactly one required**):
- `--role-id`, `-r` *(optional unless `--name` is omitted)*: The GUID of the security role to analyze.
- `--name`, `-n` *(optional unless `--role-id` is omitted)*: The name of the security role to analyze.

Validation:
- You **cannot** use `--role-id` and `--name` together.
- You **must** provide one of them.

---

#### `sec list-user-roles`
This command is registered in the application, but its detailed option configuration was not available in the retrieved results for this update.

---

### `flow` — Commands for analysis and utilities regarding cloud flows.

All flow subcommands are **solution-aware** and share these options:

Shared options:
- `--online-solution`, `-s` *(optional)*: Unique name of the solution to download from the online environment.
- `--dir`, `-d` *(optional)*: Path to an unzipped, already downloaded solution.
- `--force-download`, `-f` *(optional, boolean; default: false)*: Force downloading a new version of the solution instead of using the cache.

> The command will resolve the actual solution path before analyzing.

#### `flow column-dependency` — Check all flows in a solution for dependencies on a specific entity field.

Options:
- `--entity`, `-e` *(required)*: The logical singular name of the entity to check.
- `--column`, `-c` *(required)*: The logical name of the column to check for dependencies.
- `--action`, `-a` *(optional)*: Action filter to apply.
- `--actions-only`, `-ao` *(optional, boolean; default: false)*: If set, only actions will be included.
- `--triggers-only`, `-to` *(optional, boolean; default: false)*: If set, only triggers will be included.

Validation:
- You cannot use `--actions-only` and `--triggers-only` together.

---

#### `flow triggered-by-message` — Check all flows in a solution for triggers on a specific entity and message.

Options:
- `--entity`, `-e` *(required)*: The logical singular name of the entity to check.
- `--message`, `-m` *(required)*: The name of the event message.

Validation:
- `--message` must be one of: `create`, `update`, `delete` (case-insensitive).

---

#### `flow triggers-message` — Check all flows in a solution for triggering specific entity messages (e.g. create account).

Options:
- `--entity`, `-e` *(required)*: The logical singular name of the entity targeted by the action.
- `--message`, `-m` *(required)*: The type of operation to search for actions performing.

Validation:
- `--message` must be one of the allowed operation names defined by the tool (derived from `FlowAttributes.ActionNameToOperationIdMap`).

---

### `form`

#### `form script-handler-analysis` — Analyze form scripts for registered JavaScript handlers.

Options:
- `--entity`, `-e` *(required)*: The logical name of the entity.
- `--formId`, `-f` *(required)*: The GUID of the form to analyze.

Notes:
- This command requires an active Dataverse connection.

---

### `solution` — Commands for analyzing and interacting with solutions.

#### `solution compare` — Compare two solution sets to identify differences and similarities.

Options:
- `--env1`, `-e1` *(optional)*: Environment name for the first solution set (optional; uses active connection if not specified).
- `--env2`, `-e2` *(optional)*: Environment name for the second solution set (optional; uses active connection if not specified).
- `--solutions1`, `-s1` *(required)*: The first set of solutions to compare, specified as comma-separated list.
- `--solutions2`, `-s2` *(required)*: The second set of solutions to compare, specified as comma-separated list.

Validation:
- `--env1` and `--env2` must be specified together, or neither should be specified (to use the active connection).

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