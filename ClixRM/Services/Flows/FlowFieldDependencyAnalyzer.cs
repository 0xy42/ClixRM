using System.Text.Json;

namespace ClixRM.Services.Flows;

public record FieldDependencyResult(
    string FileName,
    string SourceType,
    string SourceName,
    string DependencyType,
    string EntityName,
    string FieldName,
    string Details
);


/// <summary>
///     Analyzes Power Automate flows to find dependencies on specific entity fields.
/// </summary>
public class FlowFieldDependencyAnalyzer : FlowAnalyzerBase
{
    private const string FieldParameterPrefix = "item/";

    /// <summary>
    ///     Analyzes field dependencies in workflows within a solution directory.
    /// </summary>
    /// <param name="solutionDirectory">Path to the unzipped solution directory.</param>
    /// <param name="entityName">Logical name of the entity (singular preferred).</param>
    /// <param name="fieldName">Logical name of the field.</param>
    /// <param name="actionTypeFilter">Optional filter to include only actions of a specific type.</param>
    /// <param name="actionsOnly">If true, only search within actions.</param>
    /// <param name="triggersOnly">If true, only search within triggers.</param>
    /// <returns>List of FieldDependencyResult objects describing workflows using the specified field.</returns>
    public List<FieldDependencyResult> AnalyzeFieldUsage(string solutionDirectory, string entityName, string fieldName, string? actionTypeFilter, bool actionsOnly, bool triggersOnly) // Changed return type
    {
        if (string.IsNullOrWhiteSpace(solutionDirectory))
            throw new ArgumentException("Solution directory cannot be null or empty.", nameof(solutionDirectory));
        if (!Directory.Exists(solutionDirectory))
            throw new DirectoryNotFoundException($"The specified solution directory '{solutionDirectory}' does not exist.");
        if (string.IsNullOrWhiteSpace(entityName))
            throw new ArgumentException("Entity name cannot be null or empty.", nameof(entityName));
        if (string.IsNullOrWhiteSpace(fieldName))
            throw new ArgumentException("Field name cannot be null or empty.", nameof(fieldName));

        var pluralEntityName = GetPluralName(entityName);

        var workflowFiles = ReadWorkflowFiles(solutionDirectory);
        var workflowsWithFieldUsage = new List<FieldDependencyResult>();

        foreach (var filePath in workflowFiles)
        {
            var fileNameOnly = Path.GetFileName(filePath);
            try
            {
                var fileContent = File.ReadAllText(filePath);
                using var jsonDoc = JsonDocument.Parse(fileContent);

                if (!actionsOnly)
                {
                    workflowsWithFieldUsage.AddRange(CheckTriggers(jsonDoc, entityName, pluralEntityName, fieldName, fileNameOnly));
                }

                if (!triggersOnly)
                {
                    workflowsWithFieldUsage.AddRange(CheckActions(jsonDoc, entityName, pluralEntityName, fieldName, actionTypeFilter, fileNameOnly));
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Warning: Could not parse JSON file '{fileNameOnly}'. Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Error processing file '{fileNameOnly}'. Error: {ex.Message}");
            }
        }

        return workflowsWithFieldUsage;
    }

    private static IEnumerable<FieldDependencyResult> CheckTriggers(JsonDocument jsonDoc, string singularEntityName, string pluralEntityName, string fieldName, string fileNameOnly) // Changed return type, added fileNameOnly
    {
        var triggerDetails = new List<FieldDependencyResult>();
        if (!TryGetTriggersElement(jsonDoc, out var triggers))
        {
            return triggerDetails;
        }

        foreach (var trigger in triggers.EnumerateObject())
        {
            ProcessTrigger(trigger, singularEntityName, pluralEntityName, fieldName, triggerDetails, fileNameOnly);
        }
        return triggerDetails;
    }

    private static void ProcessTrigger(JsonProperty trigger, string singularEntityName, string pluralEntityName, string fieldName, List<FieldDependencyResult> triggerDetails, string fileNameOnly)
    {
        if (!trigger.Value.TryGetProperty("inputs", out var inputs) ||
            !inputs.TryGetProperty("parameters", out var parameters))
        {
            return;
        }

        if (!parameters.TryGetProperty("subscriptionRequest/entityname", out var triggerEntityNameElement) ||
            triggerEntityNameElement.ValueKind != JsonValueKind.String)
        {
            return;
        }

        var triggerEntityNameString = triggerEntityNameElement.GetString()!;
        var entityMatches = triggerEntityNameString.Equals(singularEntityName, DefaultStringComparison) ||
                            triggerEntityNameString.Equals(pluralEntityName, DefaultStringComparison);

        if (!entityMatches)
        {
            return;
        }

        if (parameters.TryGetProperty("subscriptionRequest/filteringattributes", out var filterAttrElement) &&
            filterAttrElement.ValueKind == JsonValueKind.String)
        {
            var filteringAttributes = filterAttrElement.GetString()?.Split(',');
            if (filteringAttributes?.Contains(fieldName, StringComparer.OrdinalIgnoreCase) == true)
            {
                var message = GetTranslatedMetadata(parameters, "subscriptionRequest/message", TranslateMessage);
                var scope = GetTranslatedMetadata(parameters, "subscriptionRequest/scope", TranslateScope);
                var result = new FieldDependencyResult(
                    FileName: fileNameOnly,
                    SourceType: "Trigger",
                    SourceName: trigger.Name,
                    DependencyType: "Filter Attribute",
                    EntityName: singularEntityName,
                    FieldName: fieldName,
                    Details: $"Message: {message}, Scope: {scope}"
                );
                triggerDetails.Add(result);
            }
        }

        if (trigger.Value.TryGetProperty("conditions", out var conditions) &&
            DoesJsonContainsFieldReference(conditions.ToString(), fieldName))
        {
            var result = new FieldDependencyResult(
               FileName: fileNameOnly,
               SourceType: "Trigger",
               SourceName: trigger.Name,
               DependencyType: "Condition",
               EntityName: singularEntityName,
               FieldName: fieldName,
               Details: "Field referenced in condition logic (manual check recommended)"
            );

            if (!triggerDetails.Any(r => r.SourceName == trigger.Name && r.FieldName == fieldName && r.DependencyType == "Filter Attribute"))
            {
                triggerDetails.Add(result);
            }
        }
    }

    private static IEnumerable<FieldDependencyResult> CheckActions(JsonDocument jsonDoc, string singularEntityName, string pluralEntityName, string fieldName, string? actionTypeFilter, string fileNameOnly)
    {
        var actionDetails = new List<FieldDependencyResult>();

        if (!TryGetActionsElement(jsonDoc, out var actions))
        {
            return actionDetails;
        }

        foreach (var action in actions.EnumerateObject())
        {
            ProcessAction(action, singularEntityName, pluralEntityName, fieldName, actionTypeFilter, actionDetails, fileNameOnly);
        }
        return actionDetails;
    }

    private static void ProcessAction(JsonProperty action, string singularEntityName, string pluralEntityName, string fieldName, string? actionTypeFilter, List<FieldDependencyResult> actionDetails, string fileNameOnly)
    {
        var actionName = action.Name;
        var actionValue = action.Value;
        var actionType = GetActionType(actionValue);

        if (!string.IsNullOrWhiteSpace(actionTypeFilter) && !actionType.Equals(actionTypeFilter, DefaultStringComparison))
        {
            return;
        }

        var processedInput = false;

        if (actionValue.TryGetProperty("inputs", out var inputs))
        {
            if (inputs.ValueKind == JsonValueKind.Object)
            {
                if (inputs.TryGetProperty("parameters", out var parameters) && parameters.ValueKind == JsonValueKind.Object)
                {
                    CheckActionParameters(parameters, actionName, actionType, singularEntityName, pluralEntityName, fieldName, actionDetails, fileNameOnly);
                    processedInput = true;
                }
            }
            else if (inputs.ValueKind == JsonValueKind.String)
            {
                CheckInputStringForFieldReference(inputs.GetString(), actionName, actionType, fieldName, actionDetails, fileNameOnly);
                processedInput = true;
            }
        }

        if (!processedInput)
        {
            CheckActionJsonForFieldReference(actionValue, actionName, actionType, fieldName, actionDetails, fileNameOnly);
        }
    }

    private static void CheckInputStringForFieldReference(string? inputString, string actionName, string actionType, string fieldName, List<FieldDependencyResult> actionDetails, string fileNameOnly)
    {
        if (inputString == null || !DoesJsonContainsFieldReference(inputString, fieldName)) return;

        var result = new FieldDependencyResult(
            FileName: fileNameOnly,
            SourceType: "Action",
            SourceName: actionName,
            DependencyType: "Input String",
            EntityName: "Unknown/Contextual",
            FieldName: fieldName,
            Details: $"Type: {actionType}, Field referenced in input content/expression"
        );
        actionDetails.Add(result);
    }

    private static void CheckActionParameters(JsonElement parameters, string actionName, string actionType, string singularEntityName, string pluralEntityName, string fieldName, List<FieldDependencyResult> actionDetails, string fileNameOnly)
    {
        var entityMatchesExplicitly = false;
        var explicitEntityName = "Unknown/Contextual";

        if (parameters.TryGetProperty("entityName", out var entityNameElement) &&
            entityNameElement.ValueKind == JsonValueKind.String)
        {
            var matchedEntityNameInJson = entityNameElement.GetString()!;
            if (matchedEntityNameInJson.Equals(singularEntityName, DefaultStringComparison) ||
                matchedEntityNameInJson.Equals(pluralEntityName, DefaultStringComparison))
            {
                entityMatchesExplicitly = true;
                explicitEntityName = singularEntityName;
            }
        }

        foreach (var parameter in parameters.EnumerateObject())
        {
            if (entityMatchesExplicitly)
            {
                CheckParameterForDirectFieldUsage(parameter, fieldName, actionName, actionType, explicitEntityName, actionDetails, fileNameOnly);
            }

            CheckParameterValueForFieldReference(parameter, fieldName, actionName, actionType, singularEntityName, pluralEntityName, entityMatchesExplicitly, explicitEntityName, actionDetails, fileNameOnly);
        }
    }

    private static void CheckParameterForDirectFieldUsage(JsonProperty parameter, string fieldName, string actionName, string actionType, string explicitEntityName, List<FieldDependencyResult> actionDetails, string fileNameOnly)
    {
        if (!parameter.Name.StartsWith(FieldParameterPrefix, DefaultStringComparison)) return;

        var potentialFieldName = parameter.Name[FieldParameterPrefix.Length..];

        if (!potentialFieldName.Equals(fieldName, DefaultStringComparison)) return;

        var result = new FieldDependencyResult(
            FileName: fileNameOnly,
            SourceType: "Action",
            SourceName: actionName,
            DependencyType: "Direct Parameter",
            EntityName: explicitEntityName,
            FieldName: fieldName,
            Details: $"Type: {actionType}"
        );
        actionDetails.Add(result);
    }

    private static void CheckParameterValueForFieldReference(JsonProperty parameter, string fieldName, string actionName, string actionType, string singularEntityName, string pluralEntityName, bool entityMatchesExplicitly, string explicitEntityName, List<FieldDependencyResult> actionDetails, string fileNameOnly)
    {
        if (parameter.Value.ValueKind != JsonValueKind.String) return;

        var paramValueString = parameter.Value.GetString();

        if (paramValueString == null || !DoesJsonContainsFieldReference(paramValueString, fieldName)) return;

        var inferredEntity = entityMatchesExplicitly ? explicitEntityName : InferEntityFromContext(parameter.Name, singularEntityName, pluralEntityName);

        var result = new FieldDependencyResult(
            FileName: fileNameOnly,
            SourceType: "Action",
            SourceName: actionName,
            DependencyType: "Expression/Value",
            EntityName: inferredEntity,
            FieldName: fieldName,
            Details: $"Type: {actionType}, Parameter: '{parameter.Name}'"
        );

        if (!actionDetails.Any(r => r.SourceName == actionName && r.FieldName == fieldName && r.DependencyType == "Direct Parameter"))
        {
            actionDetails.Add(result);
        }
    }

    private static void CheckActionJsonForFieldReference(JsonElement actionValue, string actionName, string actionType, string fieldName, List<FieldDependencyResult> actionDetails, string fileNameOnly)
    {
        var actionJsonString = actionValue.ToString();
        if (!DoesJsonContainsFieldReference(actionJsonString, fieldName)) return;

        var result = new FieldDependencyResult(
            FileName: fileNameOnly,
            SourceType: "Action",
            SourceName: actionName,
            DependencyType: "Fallback/Unknown",
            EntityName: "Unknown/Contextual",
            FieldName: fieldName,
            Details: $"Type: {actionType}, Field reference found in action structure/expression"
        );

        if (!actionDetails.Any(r => r.SourceName == actionName && r.FieldName == fieldName))
        {
            actionDetails.Add(result);
        }
    }

    /// <summary>
    ///     Performs simple text search within a string for potential field name references.
    ///     NOTE: This is heuristic and specific to finding field names.
    /// </summary>
    private static bool DoesJsonContainsFieldReference(string? jsonString, string fieldName)
    {
        if (string.IsNullOrEmpty(jsonString)) return false;

        return jsonString.IndexOf($"'{fieldName}'", DefaultStringComparison) >= 0 ||
               jsonString.IndexOf($"/{fieldName}", DefaultStringComparison) >= 0 ||
               jsonString.IndexOf($"({fieldName})", DefaultStringComparison) >= 0 ||
               jsonString.IndexOf($"[\"{fieldName}\"]", DefaultStringComparison) >= 0 ||
               jsonString.IndexOf($"['{fieldName}']", DefaultStringComparison) >= 0;
    }
}