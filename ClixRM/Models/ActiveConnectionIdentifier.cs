namespace ClixRM.Models;

public record ActiveConnectionIdentifier(
    string EnvironmentName,
    Guid ConnectionId
);
