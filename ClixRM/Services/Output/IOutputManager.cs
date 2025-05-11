namespace ClixRM.Services.Output;

public interface IOutputManager
{
    /// <summary>
    ///     Prints a standard informational message to the console.
    /// </summary>
    /// <param name="message">The message to print.</param>
    void PrintInfo(string message);

    /// <summary>
    ///     Prints a success message to the console (typically in green).
    /// </summary>
    /// <param name="message">The message to print.</param>
    void PrintSuccess(string message);

    /// <summary>
    ///     Prints a warning message to the console (typically in yellow or dark yellow).
    /// </summary>
    /// <param name="message">The message to print.</param>
    void PrintWarning(string message);

    /// <summary>
    ///     Prints an error message to the console (typically in red).
    /// </summary>
    /// <param name="message">The message to print.</param>
    void PrintError(string message);
}