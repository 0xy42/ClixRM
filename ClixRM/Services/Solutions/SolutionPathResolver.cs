using ClixRM.Sdk.Services;

namespace ClixRM.Services.Solutions;


public class SolutionPathResolver : ISolutionPathResolver
{
    private readonly ISolutionDownloader _solutionDownloader;
    private readonly IOutputManager _outputManager;

    public SolutionPathResolver(ISolutionDownloader solutionDownloader, IOutputManager outputManager)
    {
        _solutionDownloader = solutionDownloader;
        _outputManager = outputManager;
    }

    public async Task<string?> ResolveSolutionPathAsync(string? onlineSolutionName, string? directoryPath, bool forceDownload)
    {
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

        string actualSolutionPathToAnalyze;

        try
        {
            if (!string.IsNullOrEmpty(onlineSolutionName))
            {
                if (forceDownload)
                {
                    _outputManager.PrintInfo($"Force downloading and unpacking solution '{onlineSolutionName}'...");
                }
                else
                {
                    _outputManager.PrintInfo($"Attempting to get solution '{onlineSolutionName}' (downloading if not cached or cache is stale)...");
                }

                var downloadResult = await _solutionDownloader.DownloadAndUnpackSolutionAsync(onlineSolutionName, forceDownload);
                actualSolutionPathToAnalyze = downloadResult.UnpackedSolutionPath;
                _outputManager.PrintInfo($"Solution '{onlineSolutionName}' is ready for analysis at: {actualSolutionPathToAnalyze}");

                if (!forceDownload && downloadResult.DownloadTimestampUtc < DateTime.UtcNow.AddDays(-2))
                {
                    _outputManager.PrintWarning($"Note: Cached data for solution '{onlineSolutionName}' was downloaded on {downloadResult.DownloadTimestampUtc}. " +
                                              "If you need the absolute latest version, consider using the --force-download flag or clearing the cache for this solution.");
                }
            }
            else
            {
                actualSolutionPathToAnalyze = directoryPath!;
                _outputManager.PrintInfo($"Using provided directory path: {actualSolutionPathToAnalyze}");
            }
            return actualSolutionPathToAnalyze;
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
}