using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.CommandLine;

namespace ClixRM;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        Startup.ConfigureSerilog(configuration);

        try
        {
            var services = new ServiceCollection();

            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddSerilog(dispose: true);
            });

            Startup.ConfigureServices(services, configuration);

            var serviceProvider = services.BuildServiceProvider();

            var rootCommand = serviceProvider.GetRequiredService<RootCommand>();
            return await rootCommand.InvokeAsync(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "ClixRM application terminated unexpectedly.");
            return 1;
        }
        finally
        {
            Log.Information("ClixRM application shutting down...");
            await Log.CloseAndFlushAsync();
        }
    }
}