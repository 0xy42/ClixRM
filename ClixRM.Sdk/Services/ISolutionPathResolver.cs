namespace ClixRM.Sdk.Services
{
    /// <summary>
    ///     Resolves the path for solutions to analyze, either online in Dataverse or in a local directory path.
    /// </summary>
    public interface ISolutionPathResolver
    {
        /// <summary>
        ///     Resolve the path for a solution to analyzer.
        /// </summary>
        /// <param name="onlineSolutionName">The logical name of the online solution to analyze.</param>
        /// <param name="directoryPath">The local directory path to the unpacked, downloaded solution to analyze.</param>
        /// <param name="forceDownload">Enforce the download of an online solution, even if a valid cache exists.</param>
        /// <returns>The path to the existing or downloaded solution.</returns>
        Task<string?> ResolveSolutionPathAsync(string? onlineSolutionName, string? directoryPath, bool forceDownload);
    }
}
