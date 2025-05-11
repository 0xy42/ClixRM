namespace ClixRM.Services.Output;

public class OutputManager : IOutputManager
{
    private readonly object _consoleLock = new object();

    public void PrintInfo(string message)
    {
        lock (_consoleLock)
        {
            Console.ResetColor();
            Console.WriteLine(message);
        }
    }

    public void PrintSuccess(string message)
    {
        lock (_consoleLock)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }

    public void PrintWarning(string message)
    {
        lock (_consoleLock)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }

    public void PrintError(string message)
    {
        lock (_consoleLock)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}