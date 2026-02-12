using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace ClixRM.Services.Solutions;

public interface IComponentMetadataService
{
    Task<Dictionary<int, string>> GetComponentTypeMapAsync(IOrganizationServiceAsync2 service);
}
