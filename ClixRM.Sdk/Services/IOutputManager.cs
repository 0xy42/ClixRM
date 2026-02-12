namespace ClixRM.Sdk.Services;

/// <summary>
///     Manager for printing output thread safe in the terminal.
/// </summary>
public interface IOutputManager
{
    /// <summary>
    ///     Prints a standard informational message to the console.
    /// </summary>
    /// <param name="message">The message to print.</param>
    void PrintInfo(string message);

    /// <summary>
    ///     Prints a success message to the console (in green).
    /// </summary>
    /// <param name="message">The message to print.</param>
    void PrintSuccess(string message);

    /// <summary>
    ///     Prints a warning message to the console (in dark yellow).
    /// </summary>
    /// <param name="message">The message to print.</param>
    void PrintWarning(string message);

    /// <summary>
    ///     Prints an error message to the console (in red).
    /// </summary>
    /// <param name="message">The message to print.</param>
    void PrintError(string message);
}