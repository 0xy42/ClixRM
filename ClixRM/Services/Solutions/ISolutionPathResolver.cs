namespace ClixRM.Services.Solutions
{
    public interface ISolutionPathResolver
    {
        Task<string?> ResolveSolutionPathAsync(string? onlineSolutionName, string? directoryPath, bool forceDownload);
    }
}
