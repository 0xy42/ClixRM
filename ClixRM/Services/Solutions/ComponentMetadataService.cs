using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace ClixRM.Services.Solutions;

public class ComponentMetadataService : IComponentMetadataService
{
    private readonly ILogger<ComponentMetadataService> _logger;
    private Dictionary<int, string>? _cachedComponentTypes; 

    public ComponentMetadataService(ILogger<ComponentMetadataService> logger)
    {
        _logger = logger;
    }

    public async Task<Dictionary<int, string>> GetComponentTypeMapAsync(IOrganizationServiceAsync2 service)
    {
        if (_cachedComponentTypes != null)
        {
            return _cachedComponentTypes;
        }

        try
        {
            var request = new RetrieveAttributeRequest
            {
                EntityLogicalName = "solutioncomponent",
                LogicalName = "componenttype",
                RetrieveAsIfPublished = true,
            };

            var response = (RetrieveAttributeResponse)await service.ExecuteAsync(request);

            if (response.AttributeMetadata is not PicklistAttributeMetadata picklistMetadata)
            {
                _logger.LogError("componenttype is not a PicklistAttributeMetadata");
                throw new InvalidOperationException($"Retrieved metadata is not of type {nameof(PicklistAttributeMetadata)}.");
            }

            _cachedComponentTypes = [];

            foreach (var option in picklistMetadata.OptionSet.Options)
            {
                if (!option.Value.HasValue)
                {
                    continue;
                }

                var label = option.Label?.UserLocalizedLabel?.Label ?? $"UNKNOWN ({option.Value})";
                _cachedComponentTypes[option.Value.Value] = label;
            }

            _logger.LogInformation("Retrieved {Count} component types from metadata", _cachedComponentTypes);

            return _cachedComponentTypes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve component type metadata");
            throw;
        }
    }
}
