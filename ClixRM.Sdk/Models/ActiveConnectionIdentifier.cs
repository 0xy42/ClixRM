namespace ClixRM.Sdk.Models;

public record ActiveConnectionIdentifier(
    string EnvironmentName,
    Guid ConnectionId
);
