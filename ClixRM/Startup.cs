using ClixRM.Commands;
using ClixRM.Commands.Auth;
using ClixRM.Commands.Flows;
using ClixRM.Commands.Forms;
using ClixRM.Commands.Security;
using ClixRM.Commands.Solution;
using ClixRM.Formatter;
using ClixRM.Models;
using ClixRM.Models.Solutions;
using ClixRM.Sdk.Services;
using ClixRM.Services.Authentication;
using ClixRM.Services.Flows;
using ClixRM.Services.Forms;
using ClixRM.Services.Output;
using ClixRM.Services.Security;
using ClixRM.Services.Solutions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Settings.Configuration;
using Serilog.Sinks.File;
using System.CommandLine;

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
        services.AddTransient<ListUserRolesCommand>();
        services.AddTransient<UsersWithRoleCommand>();
        services.AddTransient<SecurityCommand>();

        // Flow
        services.AddTransient<ColumnDependencyCheckCommand>();
        services.AddTransient<FlowTriggeredByEntityMessageCommand>();
        services.AddTransient<FlowTriggersEntityMessageCommand>();
        services.AddTransient<FlowCommand>();

        // Form
        services.AddTransient<ScriptHandlerAnalysisCommand>();
        services.AddTransient<FormCommand>();

        // Solutions
        services.AddTransient<SolutionCommand>();
        services.AddTransient<SolutionComparerCommand>();

        // services
        services.AddSingleton<IOutputManager, OutputManager>();
        services.AddSingleton<IDataverseConnector, DataverseConnector>();
        services.AddSingleton<ISecurityRoleAnalyzer, SecurityRoleAnalyzer>();
        services.AddSingleton<ISecureStorage, SecureStorage>();
        services.AddSingleton<IActiveConnectionGuard, ActiveConnectionGuard>();
        services.AddTransient<ISolutionDownloader, SolutionDownloader>();
        services.AddTransient<ISolutionPathResolver, SolutionPathResolver>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IFormAnalyzer, FormAnalyzer>();
        services.AddSingleton<ISolutionComparer, SolutionComparer>();
        services.AddSingleton<IComponentMetadataService, ComponentMetadataService>();

        // formatters
        services.AddTransient<ICommandResultFormatter<SolutionComparisonResult>, SolutionComparisonCommandFormatter>();
        services.AddTransient<ICommandResultFormatter<List<SecurityRoleCheckResult>>, ListUserRolesCommandFormatter>();
        services.AddTransient<ICommandResultFormatter<List<PrivilegeCheckResult>>, PrivilegeCheckCommandFormatter>();
        services.AddTransient<ICommandResultFormatter<List<UserWithRoleResult>>, UserWithRoleCommandFormatter>();
        services.AddTransient<ICommandResultFormatter<FormAnalysisResult>, ScriptHandlerAnalysisCommandFormatter>(); 
        services.AddTransient<ICommandResultFormatter<List<FieldDependencyResult>>, ColumnDependencyCheckCommandFormatter>();  
        services.AddTransient<ICommandResultFormatter<List<TriggeredByEntityMessageResult>>, FlowTriggeredByEntityMessageCommandFormatter>();  
        services.AddTransient<ICommandResultFormatter<List<FlowTriggersEntityMessageResult>>, FlowTriggersEntityMessageCommandFormatter>();


        // setup root command
        services.AddSingleton(provider =>
        {
            var rootCommand = new RootCommand("A CLI Helper tool for various actions and utilities and analysis in Dynamics XRM.");
            rootCommand.AddCommand(provider.GetRequiredService<AuthCommand>());
            rootCommand.AddCommand(provider.GetRequiredService<SecurityCommand>());
            rootCommand.AddCommand(provider.GetRequiredService<FlowCommand>());
            rootCommand.AddCommand(provider.GetRequiredService<FormCommand>());
            rootCommand.AddCommand(provider.GetRequiredService<SolutionCommand>());
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