namespace ClixRM.FirstParty.Services.Flows;

public class FlowAttributes
{
    public static readonly Dictionary<string, List<string>> ActionNameToOperationIdMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "create", ["CreateRecord", "UpdateRecord"] },
        { "update", ["UpdateOnlyRecord", "UpdateRecord"] },
        { "delete", ["DeleteRecord"] },
        { "list", ["ListRecords"] },
        { "get", ["GetItem"] }
    };
}