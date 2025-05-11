using System.Text.Json;

namespace ClixRM.Services.Flows;

public record FlowTriggersEntityMessageResult(
    string ActionName,
    string ActionType,
    string OperationId,
    string EntityName,
    string FileName
);

/// <summary>
///     Analyzes Power Automate flows to find actions performing specific operations on entities.
/// </summary>
public class FlowTriggersEntityMessageAnalyzer : FlowAnalyzerBase
{
    public static readonly Dictionary<string, List<string>> ActionNameToOperationIdMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "create", ["CreateRecord", "UpdateRecord"] },
        { "update", ["UpdateOnlyRecord", "UpdateRecord"] },
        { "delete", ["DeleteRecord"] },
        { "list", ["ListRecords"] },
        { "get", ["GetItem"] }
        // Add more mappings as needed
    };

    public List<FlowTriggersEntityMessageResult> AnalyzeActionUsage(string solutionDirectory, string entityName, string targetActionName)
    {
        if (string.IsNullOrWhiteSpace(solutionDirectory))
            throw new ArgumentException("Solution directory cannot be null or empty.", nameof(solutionDirectory));
        if (!Directory.Exists(solutionDirectory))
            throw new DirectoryNotFoundException($"The specified solution directory '{solutionDirectory}' does not exist.");
        if (string.IsNullOrWhiteSpace(entityName))
            throw new ArgumentException("Entity name cannot be null or empty.", nameof(entityName));
        if (string.IsNullOrWhiteSpace(targetActionName))
            throw new ArgumentException("Target action name cannot be null or empty.", nameof(targetActionName));

        if (!ActionNameToOperationIdMap.TryGetValue(targetActionName, out var targetOperationIds) || targetOperationIds == null || !targetOperationIds.Any())
        {
            Console.WriteLine($"Warning: Action name '{targetActionName}' not recognized or has no mapped operations. Allowed keys are: {string.Join(", ", ActionNameToOperationIdMap.Keys)}");
            return [];
        }

        var pluralEntityName = GetPluralName(entityName);
        var workflowFiles = ReadWorkflowFiles(solutionDirectory);
        var workflowsWithMatchingActions = new List<FlowTriggersEntityMessageResult>();

        foreach (var filePath in workflowFiles)
        {
            var fileNameOnly = Path.GetFileName(filePath);
            try
            {
                var fileContent = File.ReadAllText(filePath);
                using var jsonDoc = JsonDocument.Parse(fileContent);

                if (jsonDoc.RootElement.TryGetProperty("properties", out var propertiesElement) &&
                    propertiesElement.TryGetProperty("definition", out var definitionElement) &&
                    definitionElement.TryGetProperty("actions", out var topLevelActionsElement) &&
                    topLevelActionsElement.ValueKind == JsonValueKind.Object)
                {
                    FindActionsRecursively(topLevelActionsElement, entityName, pluralEntityName, targetOperationIds, fileNameOnly, workflowsWithMatchingActions);
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
        return workflowsWithMatchingActions;
    }

    /// <summary>
    ///     Recursively finds actions within a given scope (e.g., top-level, inside an If/Else, Scope, ForEach).
    /// </summary>
    private static void FindActionsRecursively(
        JsonElement currentActionsScope,
        string singularEntityName,
        string pluralEntityName,
        List<string> targetOperationIds,
        string fileNameOnly,
        List<FlowTriggersEntityMessageResult> accumulatedResults)
    {
        if (currentActionsScope.ValueKind != JsonValueKind.Object) return;

        foreach (var actionProperty in currentActionsScope.EnumerateObject())
        {
            var actionNameKey = actionProperty.Name;
            var actionValue = actionProperty.Value;

            ProcessSingleActionNode(actionNameKey, actionValue, singularEntityName, pluralEntityName, targetOperationIds, accumulatedResults, fileNameOnly);

            if (actionValue.TryGetProperty("actions", out var nestedActions) && nestedActions.ValueKind == JsonValueKind.Object)
            {
                FindActionsRecursively(nestedActions, singularEntityName, pluralEntityName, targetOperationIds, fileNameOnly, accumulatedResults);
            }

            if (actionValue.TryGetProperty("else", out var elseBranch) &&
                elseBranch.TryGetProperty("actions", out var elseActions) && elseActions.ValueKind == JsonValueKind.Object)
            {
                FindActionsRecursively(elseActions, singularEntityName, pluralEntityName, targetOperationIds, fileNameOnly, accumulatedResults);
            }

            if (actionValue.TryGetProperty("cases", out var casesElement) && casesElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var caseProperty in casesElement.EnumerateObject())
                {
                    if (caseProperty.Value.TryGetProperty("actions", out var caseActions) && caseActions.ValueKind == JsonValueKind.Object)
                    {
                        FindActionsRecursively(caseActions, singularEntityName, pluralEntityName, targetOperationIds, fileNameOnly, accumulatedResults);
                    }
                }
            }
        }
    }

    /// <summary>
    ///     Processes a single action node to see if it matches the criteria.
    /// </summary>
    private static void ProcessSingleActionNode(
        string actionNameKey,
        JsonElement actionValue,
        string singularEntityName,
        string pluralEntityName,
        List<string> targetOperationIds,
        List<FlowTriggersEntityMessageResult> accumulatedResults,
        string fileNameOnly)
    {
        if (!actionValue.TryGetProperty("type", out var typeElement) ||
            typeElement.GetString() != "OpenApiConnection")
        {
            return;
        }

        if (!actionValue.TryGetProperty("inputs", out var inputs) || inputs.ValueKind != JsonValueKind.Object ||
            !inputs.TryGetProperty("host", out var host) || host.ValueKind != JsonValueKind.Object ||
            !host.TryGetProperty("operationId", out var operationIdElement) || operationIdElement.ValueKind != JsonValueKind.String)
        {
            return;
        }
        var actualOperationId = operationIdElement.GetString();

        if (actualOperationId == null || !targetOperationIds.Contains(actualOperationId, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        if (!inputs.TryGetProperty("parameters", out var parameters) || parameters.ValueKind != JsonValueKind.Object ||
            !parameters.TryGetProperty("entityName", out var entityNameElement) || entityNameElement.ValueKind != JsonValueKind.String)
        {
            return;
        }
        var actualEntityName = entityNameElement.GetString();

        if (actualEntityName == null ||
            (!actualEntityName.Equals(singularEntityName, DefaultStringComparison) &&
             !actualEntityName.Equals(pluralEntityName, DefaultStringComparison)))
        {
            return;
        }

        var actionType = GetActionType(actionValue);
        var result = new FlowTriggersEntityMessageResult(
            ActionName: actionNameKey,
            ActionType: actionType,
            OperationId: actualOperationId,
            EntityName: singularEntityName,
            FileName: fileNameOnly
        );
        accumulatedResults.Add(result);
    }
}