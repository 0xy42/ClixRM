using System.Text.Json;

namespace ClixRM.Services.Flows;

public record TriggeredByEntityMessageResult(
    string TriggerName,
    string EventName,
    string Scope,
    string EntityName,
    string FileName
);

/// <summary>
///     Analyzes Power Automate flows to find triggers based on specific entity events.
/// </summary>
public class FlowTriggeredByEntityMessageAnalyzer : FlowAnalyzerBase
{
    /// <summary>
    /// Analyzes workflows in a solution directory to find triggers matching the specified entity and event.
    /// </summary>
    /// <param name="solutionDirectory">Path to the unzipped solution directory.</param>
    /// <param name="entityName">Logical name of the entity (singular preferred).</param>
    /// <param name="targetEventName">The name of the event (e.g., "Update", "Create", "Delete"). Case-insensitive.</param>
    /// <returns>List of TriggeredByEntityMessageResult objects describing workflows with matching triggers.</returns>
    public List<TriggeredByEntityMessageResult> AnalyzeTriggerUsage(string solutionDirectory, string entityName, string targetEventName) // Changed return type
    {
        if (string.IsNullOrWhiteSpace(solutionDirectory))
            throw new ArgumentException("Solution directory cannot be null or empty.", nameof(solutionDirectory));
        if (!Directory.Exists(solutionDirectory))
            throw new DirectoryNotFoundException($"The specified solution directory '{solutionDirectory}' does not exist.");
        if (string.IsNullOrWhiteSpace(entityName))
            throw new ArgumentException("Entity name cannot be null or empty.", nameof(entityName));
        if (string.IsNullOrWhiteSpace(targetEventName))
            throw new ArgumentException("Event name cannot be null or empty.", nameof(targetEventName));

        var pluralEntityName = GetPluralName(entityName);
        var targetEventCodes = GetMatchingEventCodes(targetEventName);

        if (targetEventCodes.Count == 0)
        {
            Console.WriteLine($"Warning: Event name '{targetEventName}' not recognized or no related events found.");
            return [];
        }

        var workflowFiles = ReadWorkflowFiles(solutionDirectory);
        var workflowsWithMatchingTriggers = new List<TriggeredByEntityMessageResult>();

        foreach (var filePath in workflowFiles)
        {
            var fileNameOnly = Path.GetFileName(filePath);
            try
            {
                var fileContent = File.ReadAllText(filePath);
                using var jsonDoc = JsonDocument.Parse(fileContent);

                workflowsWithMatchingTriggers.AddRange(CheckTriggersForEvent(jsonDoc, entityName, pluralEntityName, targetEventCodes, fileNameOnly));
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

        return workflowsWithMatchingTriggers;
    }

    /// <summary>
    ///     Finds all event codes from MessageTranslations that include the target event name.
    /// </summary>
    /// <param name="targetEventName">The event name to search for (case-insensitive).</param>
    /// <returns>A list of integer event codes.</returns>
    private static List<int> GetMatchingEventCodes(string targetEventName)
    {
        var matchingCodes = new List<int>();
        foreach (var kvp in MessageTranslations)
        {
            if (kvp.Value.Contains(targetEventName, DefaultStringComparison))
            {
                matchingCodes.Add(kvp.Key);
            }
        }
        return matchingCodes;
    }

    private static IEnumerable<TriggeredByEntityMessageResult> CheckTriggersForEvent(JsonDocument jsonDoc, string singularEntityName, string pluralEntityName, List<int> targetEventCodes, string fileNameOnly) // Changed return type and added fileNameOnly
    {
        var triggerDetails = new List<TriggeredByEntityMessageResult>();
        if (!TryGetTriggersElement(jsonDoc, out var triggers))
        {
            return triggerDetails;
        }

        foreach (var trigger in triggers.EnumerateObject())
        {
            ProcessTriggerForEvent(trigger, singularEntityName, pluralEntityName, targetEventCodes, triggerDetails, fileNameOnly);
        }
        return triggerDetails;
    }

    private static void ProcessTriggerForEvent(JsonProperty trigger, string singularEntityName, string pluralEntityName, List<int> targetEventCodes, List<TriggeredByEntityMessageResult> triggerDetails, string fileNameOnly) // Changed parameter type and added fileNameOnly
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

        if (!parameters.TryGetProperty("subscriptionRequest/message", out var messageElement) ||
            messageElement.ValueKind != JsonValueKind.Number)
        {
            return;
        }
        var triggerMessageCode = messageElement.GetInt32();

        if (!targetEventCodes.Contains(triggerMessageCode))
        {
            return;
        }

        var actualMessage = TranslateMessage(triggerMessageCode);
        var scope = GetTranslatedMetadata(parameters, "subscriptionRequest/scope", TranslateScope);

        var result = new TriggeredByEntityMessageResult(
            TriggerName: trigger.Name,
            EventName: actualMessage,
            Scope: scope,
            EntityName: singularEntityName,
            FileName: fileNameOnly
        );
        triggerDetails.Add(result);
    }
}