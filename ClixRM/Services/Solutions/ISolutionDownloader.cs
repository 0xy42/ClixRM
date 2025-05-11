namespace ClixRM.Services.Solutions;

/// <summary>
/// Defines the contract for a service that downloads and unpacks Dataverse solutions.
/// </summary>
public interface ISolutionDownloader
{
    /// <summary>
    ///     Downloads the specified solution from Dataverse, unpacks it to a local cache directory,
    ///     and saves metadata about the download.
    /// </summary>
    /// <param name="solutionUniqueName">The unique name of the solution to download.</param>
    /// <param name="forceDownload">If true, forces a re-download even if the solution exists locally.</param>
    /// <returns>A task representing the asynchronous operation, containing the result details upon successful completion.</returns>
    /// <exception cref="SolutionDownloadException">Thrown if any error occurs during download, unpack, or saving.</exception>
    /// <exception cref="ArgumentException">Thrown if solutionUniqueName is null or whitespace.</exception>
    Task<SolutionDownloadResult> DownloadAndUnpackSolutionAsync(string solutionUniqueName, bool forceDownload = false);

    /// <summary>
    ///     Gets information about a previously downloaded and unpacked solution from the local cache.
    /// </summary>
    /// <param name="solutionUniqueName">The unique name of the solution.</param>
    /// <returns>A SolutionDownloadResult containing metadata if found, otherwise null.</returns>
    SolutionDownloadResult? GetExistingSolutionInfo(string solutionUniqueName);

    /// <summary>
    ///     Gets the base path where solutions are stored.
    /// </summary>
    string GetSolutionCacheBasePath();

    /// <summary>
    ///     Clears the local cache for a specific solution.
    /// </summary>
    /// <param name="solutionUniqueName">The unique name of the solution cache to clear.</param>
    /// <returns>True if the cache directory existed and was successfully deleted, false otherwise.</returns>
    bool ClearSolutionCache(string solutionUniqueName);

    /// <summary>
    ///     Clears the entire local solution cache. Use with caution.
    /// </summary>
    /// <returns>True if the base cache directory existed and was successfully deleted, false otherwise.</returns>
    bool ClearEntireCache();
}

/// <summary>
///     Contains details about a successfully downloaded and unpacked solution.
///     This object is also serialized as metadata alongside the unpacked files.
/// </summary>
/// <param name="SolutionUniqueName">The unique name of the solution.</param>
/// <param name="UnpackedSolutionPath">The full path to the directory containing the unpacked solution files.</param>
/// <param name="DownloadTimestampUtc">The UTC timestamp when the solution download and unpack completed.</param>
/// <param name="SolutionVersion">The version of the solution (if available).</param>
public record SolutionDownloadResult(
    string SolutionUniqueName,
    string UnpackedSolutionPath,
    DateTime DownloadTimestampUtc,
    string? SolutionVersion // Making version nullable as it might not always be easy to get reliably post-export
);

/// <summary>
///     Custom exception for errors specific to the solution download process.
/// </summary>
public class SolutionDownloadException : Exception
{
    public SolutionDownloadException(string message) : base(message) { }
    public SolutionDownloadException(string message, Exception innerException) : base(message, innerException) { }
}