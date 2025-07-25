using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ClixRM.Models;
using ClixRM.Services.Authentication;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Query;

namespace ClixRM.Services.Forms;

public class FormAnalyzer : IFormAnalyzer
{
    private readonly IDataverseConnector _dataverseConnector;

    public FormAnalyzer(IDataverseConnector dataverseConnector)
    {
        _dataverseConnector = dataverseConnector;
    }

    public async Task<FormAnalysisResult> AnalyzeFormAsync(string entityName, Guid formName)
    {
        var serviceClient = await _dataverseConnector.GetServiceClientAsync();

        var formXml = await GetFormXmlAsync(serviceClient, entityName, formName);

        if (string.IsNullOrEmpty(formXml))
        {
            throw new Exception($"Form '{formName}' for entity '{entityName}' not found or has no content.");
        }

        return ParseFormXml(formXml, formName);
    }

    private async Task<string?> GetFormXmlAsync(IOrganizationServiceAsync2 service, string entityName, Guid formId)
    {
        // TODO: implement parameterization
        const int formTypeMain = 2;

        var query = new QueryExpression("systemform")
        {
            ColumnSet = new ColumnSet("formxml", "name"),
            Criteria =
            {
                FilterOperator = LogicalOperator.And,
                Conditions =
                {
                    new ConditionExpression("objecttypecode", ConditionOperator.Equal, entityName),
                    new ConditionExpression("formid", ConditionOperator.Equal, formId),
                    new ConditionExpression("type", ConditionOperator.Equal, formTypeMain)
                }
            }
        };

        var result = await service.RetrieveMultipleAsync(query);
        var formEntity = result.Entities.FirstOrDefault();

        return formEntity?.GetAttributeValue<string>("formxml");
    }

    private FormAnalysisResult ParseFormXml(string formXml, Guid formId)
    {
        var doc = XDocument.Parse(formXml);

        var libraries = new HashSet<string>();

        libraries.UnionWith(
            doc.Descendants("Library")
                .Select(lib => lib.Attribute("name")?.Value ?? string.Empty)
                .Where(name => !string.IsNullOrWhiteSpace(name))
        );

        libraries.UnionWith(
            doc.Descendants("Handler")
                .Select(h => h.Attribute("libraryName")?.Value ?? string.Empty)
                .Where(name => !string.IsNullOrWhiteSpace(name))
        );

        libraries.UnionWith(
            doc.Descendants("internaljscriptfile")
                .Select(js => js.Attribute("src")?.Value ?? string.Empty)
                .Where(src => src.StartsWith("$webresource:"))
                .Select(src => src.Replace("$webresource:", ""))
        );

        var libraryList = libraries.Select(name => new FormLibrary(name, name)).ToList();

        // Only one loop over <event>
        var eventHandlers = new List<FormEventHandler>();
        var formEvents = doc.Descendants("events").Elements("event");
        foreach (var ev in formEvents)
        {
            var eventName = ev.Attribute("name")?.Value;
            var controlId = ev.Attribute("attribute")?.Value;

            foreach (var handler in ev.Elements("Handlers").Elements("Handler"))
            {
                eventHandlers.Add(new FormEventHandler(
                    EventName: eventName ?? "",
                    FunctionName: handler.Attribute("functionName")?.Value ?? handler.Attribute("functionname")?.Value ?? string.Empty,
                    LibraryName: handler.Attribute("libraryName")?.Value ?? string.Empty,
                    Enabled: bool.TryParse(handler.Attribute("enabled")?.Value, out var enabled) ? enabled : false,
                    ControlId: controlId
                ));
            }
            foreach (var handler in ev.Elements("InternalHandlers").Elements("Handler"))
            {
                eventHandlers.Add(new FormEventHandler(
                    EventName: eventName ?? "",
                    FunctionName: handler.Attribute("functionName")?.Value ?? handler.Attribute("functionname")?.Value ?? string.Empty,
                    LibraryName: handler.Attribute("libraryName")?.Value ?? string.Empty,
                    Enabled: bool.TryParse(handler.Attribute("enabled")?.Value, out var enabled) ? enabled : false,
                    ControlId: controlId
                ));
            }
        }

        return new FormAnalysisResult
        {
            FormId = formId,
            Libraries = libraryList,
            EventHandlers = eventHandlers
        };
    }
}
