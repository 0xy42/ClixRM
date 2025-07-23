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

    public async Task<FormAnalysisResult> AnalyzeFormAsync(string entityName, string formName)
    {
        var serviceClient = await _dataverseConnector.GetServiceClientAsync();

        var formXml = await GetFormXmlAsync(serviceClient, entityName, formName);

        if (string.IsNullOrEmpty(formXml))
        {
            throw new Exception($"Form '{formName}' for entity '{entityName}' not found or has no content.");
        }

        return ParseFormXml(formXml, formName);
    }

    private async Task<string?> GetFormXmlAsync(IOrganizationServiceAsync2 service, string entityName, string formName)
    {
        // TODO: implement parameterization
        const int formTypeMain = 2;

        var query = new QueryExpression("systemform")
        {
            ColumnSet = new ColumnSet("formxml"),
            Criteria =
            {
                FilterOperator = LogicalOperator.And,
                Conditions =
                {
                    new ConditionExpression("objecttypecode", ConditionOperator.Equal, entityName),
                    new ConditionExpression("name", ConditionOperator.Equal, formName),
                    new ConditionExpression("type", ConditionOperator.Equal, formTypeMain)
                }
            }
        };

        var result = await service.RetrieveMultipleAsync(query);
        var formEntity = result.Entities.FirstOrDefault();

        return formEntity?.GetAttributeValue<string>("formxml");
    }

    private FormAnalysisResult ParseFormXml(string formXml, string formName)
    {
        var doc = XDocument.Parse(formXml);

        var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

        var libraries = doc.Descendants(ns + "library")
            .Select(lib => new FormLibrary(
                lib.Attribute("name")?.Value ?? string.Empty,
                lib.Attribute("displayName")?.Value ?? string.Empty
                ))
            .ToList();

        var eventHandlers = new List<FormEventHandler>();

        var formEvents = doc.Descendants(ns + "form").Elements(ns + "events").Elements(ns + "event");
        foreach (var ev in formEvents)
        {
            var eventName = ev.Attribute("name")?.Value;
            if (eventName == null) continue;

            foreach (var handler in ev.Elements(ns + "Handler"))
            {
                eventHandlers.Add(new FormEventHandler(
                    EventName: eventName,
                    FunctionName: handler.Attribute("functionname")?.Value ?? string.Empty,
                    LibraryName: handler.Attribute("libraryName")?.Value ?? string.Empty,
                    Enabled: bool.Parse(handler.Attribute("enabled")?.Value ?? "false")
                ));
            }
        }

        var controlEvents = doc.Descendants(ns + "control")
            .Where(c => c.Element(ns + "events") != null);

        foreach (var control in controlEvents)
        {
            var controlId = control.Attribute("id")?.Value;
            foreach (var ev in control.Elements(ns + "events").Elements(ns + "event"))
            {
                var eventName = ev.Attribute("name")?.Value;
                if (eventName == null) continue;

                foreach(var handler in ev.Elements(ns + "Handler"))
                {
                    eventHandlers.Add(new FormEventHandler(
                        EventName: eventName,
                        FunctionName: handler.Attribute("libraryName")?.Value ?? string.Empty,
                        LibraryName: handler.Attribute("libraryName")?.Value ?? string.Empty,
                        Enabled: bool.Parse(handler.Attribute("enabled")?.Value ?? "false"),
                        ControlId: controlId
                    ));
                }
            }
        }

        return new FormAnalysisResult
        {
            FormName = formName,
            Libraries = libraries,
            EventHandlers = eventHandlers
        };
    }
}
