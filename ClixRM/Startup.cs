using ClixRM.Commands.Auth;
using ClixRM.Commands.FlowCommands;
using ClixRM.Commands.Security;
using ClixRM.Services.Authentication;
using ClixRM.Services.Output;
using ClixRM.Services.Processing;
using ClixRM.Services.Security;
using ClixRM.Services.Solutions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Settings.Configuration;
using Serilog.Sinks.File;
using System.CommandLine;
using ClixRM.Commands;

namespace ClixRM;

internal static class Startup
{
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(configuration);

        //auth
        services.AddTransient<SwitchEnvironmentCommand>();
        services.AddTransient<LoginAppCommand>();
        services.AddTransient<LoginUserCommand>();
        services.AddTransient<AuthCommand>();
        services.AddTransient<ListCommand>();
        services.AddTransient<ClearCommand>();
        services.AddTransient<ShowActiveCommand>();

        // security
        services.AddTransient<PrivilegeCheckCommand>();
        services.AddTransient<ListSecurityRolesCommand>();
        services.AddTransient<SecurityCommand>();

        // Flow
        services.AddTransient<ColumnDependencyCheckCommand>();
        services.AddTransient<FlowTriggeredByEntityMessageCommand>();
        services.AddTransient<FlowTriggersEntityMessageCommand>();
        services.AddTransient<FlowCommand>();

        services.AddTransient<VersionCommand>();

        // services
        services.AddSingleton<IOutputManager, OutputManager>();
        services.AddSingleton<IDataverseConnector, DataverseConnector>();
        services.AddSingleton<ISecurityRoleAnalyzer, SecurityRoleAnalyzer>();
        services.AddSingleton<ISecureStorage, SecureStorage>();
        services.AddTransient<ISolutionDownloader, SolutionDownloader>();
        services.AddTransient<ISolutionPathResolver, SolutionPathResolver>();
        services.AddSingleton<IAuthService, AuthService>();

        services.AddSingleton(provider =>
        {
            var rootCommand = new RootCommand("A CLI Helper tool for various actions and utilities and analysis in Dynamics XRM.");
            rootCommand.AddCommand(provider.GetRequiredService<AuthCommand>());
            rootCommand.AddCommand(provider.GetRequiredService<SecurityCommand>());
            rootCommand.AddCommand(provider.GetRequiredService<FlowCommand>());
            rootCommand.AddCommand(provider.GetRequiredService<VersionCommand>());
            return rootCommand;
        });
    }

    public static void ConfigureSerilog(IConfiguration configuration)
    {
        var readerOptions = new ConfigurationReaderOptions(
            typeof(Serilog.Sinks.File.FileSink).Assembly
        );

        var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appLogDirectory = Path.Combine(localAppDataPath, "ClixRM", "Logs");
        Directory.CreateDirectory(appLogDirectory);
        var logFilePath = Path.Combine(appLogDirectory, "clixrm-.log");

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration, readerOptions)
            .Enrich.FromLogContext()
            .WriteTo.File(
                logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1)
            )
            //#if DEBUG
            //            .WriteTo.Debug(outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            //#endif
            .CreateLogger();
    }
}