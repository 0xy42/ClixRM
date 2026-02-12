using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClixRM.Models.Solutions;
using ClixRM.Sdk.Services;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace ClixRM.Services.Solutions;

public class SolutionComparer : ISolutionComparer
{
    private readonly IDataverseConnector _dataverseConnector;
    private readonly ILogger<SolutionComparer> _logger;

    public SolutionComparer(IDataverseConnector dataverseConnector, ILogger<SolutionComparer> logger)
    {
        _dataverseConnector = dataverseConnector;
        _logger = logger;
    }

    public async Task<SolutionComparisonResult> CompareSolutionsAsync(string[] solutionSet1, string[] solutionSet2)
    {
        var service = await _dataverseConnector.GetServiceClientAsync();

        _logger.LogInformation("Starting solution comparison. Set1: [{Solutions1}], Set2: [{Solutions2}]",
            string.Join(", ", solutionSet1), string.Join(", ", solutionSet2));

        var componentSet1 = await RetrieveSolutionComponentSet(service, solutionSet1);
        var componentSet2 = await RetrieveSolutionComponentSet(service, solutionSet2);

        var result = CompareSolutionSets(componentSet1, componentSet2);

        return result;
    }

    public async Task<SolutionComparisonResult> CompareSolutionsAsync(
        string environmentName1,
        string[] solutionSet1,
        string environmentName2,
        string[] solutionSet2)
    {
        _logger.LogInformation("Starting solution comparison with specified environments. Env1: {Env1}, Set1: [{Solutions1}], Env2: {Env2}, Set2: [{Solutions2}]",
            environmentName1, string.Join(", ", solutionSet1), environmentName2, string.Join(", ", solutionSet2));

        var service1 = await _dataverseConnector.GetServiceClientAsync(environmentName1);
        var service2 = await _dataverseConnector.GetServiceClientAsync(environmentName2);

        var componentSet1 = await RetrieveSolutionComponentSet(service1, solutionSet1);
        var componentSet2 = await RetrieveSolutionComponentSet(service2, solutionSet2);

        var result = CompareSolutionSets(componentSet1, componentSet2);

        return result;
    }

    private async Task<SolutionComponentSet> RetrieveSolutionComponentSet(IOrganizationServiceAsync2 service, string[] solutionNames)
    {
        _logger.LogInformation("Retrieving solution components for solutions: {SolutionNames}", string.Join(", ", solutionNames));

        var componentSet = new SolutionComponentSet
        {
            SolutionNames = solutionNames.ToList(),
            Components = new List<SolutionComponent>()
        };

        foreach (var solutionName in solutionNames)
        {
            _logger.LogDebug("Processing solution: {SolutionName}", solutionName);

            var solutionId = await RetrieveSolutionIdAsync(service, solutionName);

            if (solutionId == Guid.Empty)
            {
                _logger.LogWarning("Solution with unique name '{SolutionName}' not found. Skipping.", solutionName);
                continue;
            }

            var components = await RetrieveSolutionComponentsAsync(service, solutionId, solutionName);

            _logger.LogInformation("Retrieved {ComponentCount} components for solution '{SolutionName}'", components.Count, solutionName);

            componentSet.Components.AddRange(components);
        }

        componentSet.Components = componentSet.Components
            .DistinctBy(c => new { c.ComponentId, c.ComponentType })
            .ToList();

        _logger.LogInformation("Total unique components retrieved: {Count}", componentSet.Components.Count);

        return componentSet;
    }

    private async Task<Guid> RetrieveSolutionIdAsync(IOrganizationServiceAsync2 service, string uniqueName)
    {
        var query = new QueryExpression("solution")
        {
            ColumnSet = new ColumnSet("solutionid"),
            Criteria = new FilterExpression
            {
                Conditions =
                {
                    new ConditionExpression("uniquename", ConditionOperator.Equal, uniqueName)
                }
            },
            TopCount = 1
        };

        var results = await service.RetrieveMultipleAsync(query);

        if (results.Entities.Count == 0)
        {
            return Guid.Empty;
        }

        return results.Entities[0].Id;
    }

    private async Task<List<SolutionComponent>> RetrieveSolutionComponentsAsync(IOrganizationServiceAsync2 service, Guid solutionId, string solutionName)
    {
        var query = new QueryExpression("solutioncomponent")
        {
            ColumnSet = new ColumnSet("objectid", "componenttype"),
            Criteria = new FilterExpression
            {
                Conditions =
                {
                    new ConditionExpression("solutionid", ConditionOperator.Equal, solutionId)
                }
            }
        };

        var results = await service.RetrieveMultipleAsync(query);

        var components = new List<SolutionComponent>();

        foreach (var entity in results.Entities)
        {
            var componentId = entity.GetAttributeValue<Guid>("objectid");
            var componentType = entity.GetAttributeValue<OptionSetValue>("componenttype")?.Value ?? 0;

            components.Add(new SolutionComponent(
                ComponentId: componentId,
                ComponentType: componentType,
                LogicalName: null,
                DisplayName: null
            ));
        }

        return components;
    }

    private SolutionComparisonResult CompareSolutionSets(SolutionComponentSet set1, SolutionComponentSet set2)
    {
        _logger.LogInformation("Comparing solution sets. Set1: {Set1Count} components, Set2: {Set2Count} components", 
            set1.Components.Count, set2.Components.Count);

        var set2Lookup = set2.Components
            .ToLookup(c => (c.ComponentId, c.ComponentType));

        var set1Lookup = set1.Components
            .ToLookup(c => (c.ComponentId, c.ComponentType));

        var onlyInSet1 = set1.Components
            .Where(c => !set2Lookup.Contains((c.ComponentId, c.ComponentType)))
            .ToList();

        var onlyInSet2 = set2.Components
            .Where(c => !set1Lookup.Contains((c.ComponentId, c.ComponentType)))
            .ToList();

        var inBothSets = set1.Components
            .Where(c => set2Lookup.Contains((c.ComponentId, c.ComponentType)))
            .ToList();

        _logger.LogInformation("Comparison complete. Common: {CommonCount}, Only in Set1: {OnlyInSet1Count}, Only in Set2: {OnlyInSet2Count}",
            inBothSets.Count, onlyInSet1.Count, onlyInSet2.Count);

        return new SolutionComparisonResult
        {
            Set1 = set1,
            Set2 = set2,
            OnlyInSet1 = onlyInSet1,
            OnlyInSet2 = onlyInSet2,
            InBothSets = inBothSets
        };
    }
}
