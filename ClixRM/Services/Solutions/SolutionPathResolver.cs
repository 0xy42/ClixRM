using ClixRM.Sdk.Services;

using ClixRM.Sdk.Services;

namespace ClixRM.Services.Solutions;

public class SolutionPathResolver : ISolutionPathResolver
{
    private static readonly TimeSpan CacheStaleAfter = TimeSpan.FromDays(2);

    private readonly ISolutionDownloader _solutionDownloader;
    private readonly IOutputManager _outputManager;

    public SolutionPathResolver(ISolutionDownloader solutionDownloader, IOutputManager outputManager)
    {
        _solutionDownloader = solutionDownloader;
        _outputManager = outputManager;
    }

    public async Task<string?> ResolveSolutionPathAsync(string? onlineSolutionName, string? directoryPath, bool forceDownload)
    {
        // Guard clauses / early exits
        if (string.IsNullOrEmpty(directoryPath) && string.IsNullOrEmpty(onlineSolutionName))
        {
            _outputManager.PrintError("Error: Either --dir or --online-solution must be specified.");
            return null;
        }

        if (!string.IsNullOrEmpty(directoryPath) && !string.IsNullOrEmpty(onlineSolutionName))
        {
            _outputManager.PrintError("Error: Cannot specify both --dir and --online-solution. Please choose one.");
            return null;
        }

        try
        {
            if (!string.IsNullOrEmpty(directoryPath))
            {
                _outputManager.PrintInfo($"Using provided directory path: {directoryPath}");
                return directoryPath;
            }

            _outputManager.PrintInfo(
                forceDownload
                    ? $"Force downloading and unpacking solution '{onlineSolutionName}'..."
                    : $"Attempting to get solution '{onlineSolutionName}' (downloading if not cached or cache is stale)...");

            var downloadResult = await _solutionDownloader.DownloadAndUnpackSolutionAsync(onlineSolutionName!, forceDownload);
            _outputManager.PrintInfo($"Solution '{onlineSolutionName}' is ready for analysis at: {downloadResult.UnpackedSolutionPath}");

            PrintCacheWarningIfStale(onlineSolutionName!, forceDownload, downloadResult.DownloadTimestampUtc);

            return downloadResult.UnpackedSolutionPath;
        }
        catch (SolutionDownloadException ex)
        {
            _outputManager.PrintError($"Error obtaining solution '{onlineSolutionName}': {ex.Message}");

            if (ex.InnerException != null)
            {
                _outputManager.PrintError($"Details: {ex.InnerException.Message}");
            }

            return null;
        }
        catch (InvalidOperationException ex)
        {
            _outputManager.PrintError($"Operation failed: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            _outputManager.PrintError($"An unexpected error occurred while preparing the solution: {ex.Message}");
            return null;
        }
    }

    private void PrintCacheWarningIfStale(string onlineSolutionName, bool forceDownload, DateTime downloadTimestampUtc)
    {
        if (forceDownload)
        {
            return;
        }

        if (downloadTimestampUtc >= DateTime.UtcNow.Subtract(CacheStaleAfter))
        {
            return;
        }

        _outputManager.PrintWarning(
            $"Note: Cached data for solution '{onlineSolutionName}' was downloaded on {downloadTimestampUtc}. " +
            "If you need the absolute latest version, consider using the --force-download flag or clearing the cache for this solution.");
    }
}