using System.Text.Json;

namespace ClixRM.Services.Flows
{
    /// <summary>
    ///     Base class providing common functionality for analyzing Power Automate flow JSON files.
    /// </summary>
    public abstract class FlowAnalyzerBase
    {
        protected const StringComparison DefaultStringComparison = StringComparison.OrdinalIgnoreCase;

        protected static readonly Dictionary<int, string> MessageTranslations = new()
        {
            { 1, "Create" }, { 2, "Delete" }, { 3, "Update" }, { 4, "Create or Update" },
            { 5, "Create or Delete" }, { 6, "Update or Delete" }, { 7, "Create, Update or Delete" },
        };

        protected static readonly Dictionary<int, string> ScopeTranslations = new()
        {
            { 1, "User" }, { 2, "Business Unit" }, { 3, "Parent: Child Business Units" }, { 4, "Organization" },
        };

        protected static readonly (string Suffix, string PluralSuffix)[] PluralizationRules =
        [
            ("y", "ies"),
            ("s", "ses"),
            ("sh", "shes"),
            ("ch", "ches"),
            ("x", "xes"),
            ("z", "zes")
        ];

        /// <summary>
        /// Reads all top-level JSON files from the 'Workflows' subdirectory of the given solution directory.
        /// </summary>
        /// <param name="solutionDirectory">Path to the unzipped solution directory.</param>
        /// <returns>A list of full paths to the workflow JSON files.</returns>
        protected static List<string> ReadWorkflowFiles(string solutionDirectory)
        {
            var workflowsDir = Path.Combine(solutionDirectory, "Workflows");
            if (!Directory.Exists(workflowsDir))
            {
                // Consider throwing an exception or using a more robust logging mechanism
                Console.WriteLine($"Warning: The workflows directory '{workflowsDir}' does not exist in the solution.");
                return []; // Return empty list
            }

            var workflowFiles = Directory.GetFiles(workflowsDir, "*.json", SearchOption.TopDirectoryOnly).ToList();
            if (workflowFiles.Count == 0)
            {
                // Consider logging
                Console.WriteLine("Warning: No workflow JSON files found in the workflows directory.");
            }
            return workflowFiles;
        }

        /// <summary>
        ///     Basic English pluralization. Assumes input is singular.
        ///     Handles common cases like adding 's', 'es', and 'y' -> 'ies'.
        /// </summary>
        /// <param name="singularName">The singular noun.</param>
        /// <returns>A potential plural form of the noun.</returns>
        protected static string GetPluralName(string singularName)
        {
            if (string.IsNullOrWhiteSpace(singularName)) return singularName;

            if (singularName.EndsWith("y", StringComparison.OrdinalIgnoreCase))
            {
                if (singularName.Length > 1 && !"aeiou".Contains(singularName[^2], StringComparison.OrdinalIgnoreCase))
                {
                    return singularName[..^1] + "ies";
                }
            }

            foreach (var rule in PluralizationRules)
            {
                if (!singularName.EndsWith(rule.Suffix, StringComparison.OrdinalIgnoreCase)) continue;

                if (!singularName.EndsWith(rule.PluralSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    return singularName + (rule.PluralSuffix.StartsWith(rule.Suffix) ? rule.PluralSuffix[rule.Suffix.Length..] : rule.PluralSuffix);
                }

                return singularName;
            }

            if (!singularName.EndsWith("s", StringComparison.OrdinalIgnoreCase))
            {
                return singularName + "s";
            }

            return singularName;
        }

        /// <summary>
        ///     Attempts to navigate the standard flow JSON structure to find the 'triggers' object.
        /// </summary>
        /// <param name="jsonDoc">The parsed JsonDocument.</param>
        /// <param name="triggersElement">The resulting 'triggers' JsonElement if found.</param>
        /// <returns>True if the 'triggers' object was found, false otherwise.</returns>
        protected static bool TryGetTriggersElement(JsonDocument jsonDoc, out JsonElement triggersElement)
        {
            triggersElement = default;

            return jsonDoc?.RootElement.TryGetProperty("properties", out var properties) == true &&
                   properties.TryGetProperty("definition", out var definition) &&
                   definition.TryGetProperty("triggers", out triggersElement) &&
                   triggersElement.ValueKind == JsonValueKind.Object;
        }

        /// <summary>
        ///     Attempts to navigate the standard flow JSON structure to find the 'actions' object.
        /// </summary>
        /// <param name="jsonDoc">The parsed JsonDocument.</param>
        /// <param name="actionsElement">The resulting 'actions' JsonElement if found.</param>
        /// <returns>True if the 'actions' object was found, false otherwise.</returns>
        protected static bool TryGetActionsElement(JsonDocument jsonDoc, out JsonElement actionsElement)
        {
            actionsElement = default;

            return jsonDoc?.RootElement.TryGetProperty("properties", out var properties) == true &&
                   properties.TryGetProperty("definition", out var definition) &&
                   definition.TryGetProperty("actions", out actionsElement) &&
                   actionsElement.ValueKind == JsonValueKind.Object;
        }

        /// <summary>
        ///     Gets the 'type' property string from an action's JsonElement.
        /// </summary>
        /// <param name="actionValue">The JsonElement representing the action.</param>
        /// <returns>The action type string or "Unknown Type" if not found.</returns>
        protected static string GetActionType(JsonElement actionValue)
        {
            return actionValue.TryGetProperty("type", out var typeProp) && typeProp.ValueKind == JsonValueKind.String
                   ? typeProp.GetString() ?? "Unknown Type"
                   : "Unknown Type";
        }

        /// <summary>
        ///     Translates a trigger message code (e.g., 1 for Create) to its string representation.
        /// </summary>
        protected static string TranslateMessage(int message)
        {
            return MessageTranslations.GetValueOrDefault(message, $"Unknown ({message})");
        }

        /// <summary>
        ///     Translates a trigger scope code (e.g., 4 for Organization) to its string representation.
        /// </summary>
        protected static string TranslateScope(int scope)
        {
            return ScopeTranslations.GetValueOrDefault(scope, $"Unknown ({scope})");
        }

        /// <summary>
        ///     Helper to get and translate numeric metadata values (like message or scope) from a parameters object.
        /// </summary>
        /// <param name="parameters">The JsonElement representing the parameters object.</param>
        /// <param name="propertyName">The name of the property containing the numeric code.</param>
        /// <param name="translator">The translation function (e.g., TranslateMessage or TranslateScope).</param>
        /// <returns>The translated string or "Unknown".</returns>
        protected static string GetTranslatedMetadata(JsonElement parameters, string propertyName, Func<int, string> translator)
        {
            return parameters.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number
                   ? translator(prop.GetInt32())
                   : "Unknown";
        }

        /// <summary>
        ///     Attempts to infer an entity name based on conventions in action parameter names (e.g., _entityName_).
        ///     Returns the singular form if found.
        /// </summary>
        /// <param name="parameterName">The name of the action parameter.</param>
        /// <param name="singularEntityName">The singular entity name being searched for.</param>
        /// <param name="pluralEntityName">The plural entity name being searched for.</param>
        /// <returns>The singular entity name if context is found, otherwise "Unknown/Contextual".</returns>
        protected static string InferEntityFromContext(string parameterName, string singularEntityName, string pluralEntityName)
        {
            if (parameterName.IndexOf($"_{singularEntityName}_", StringComparison.OrdinalIgnoreCase) >= 0)
                return singularEntityName;

            if (parameterName.IndexOf($"_{pluralEntityName}_", StringComparison.OrdinalIgnoreCase) >= 0)
                return singularEntityName;

            return "Unknown/Contextual";
        }
    }
}
