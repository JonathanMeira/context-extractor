using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Termina.Hosting;

namespace ContextExtrator.CLI;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddTransient<UI.MainViewModel>();
        builder.Services.AddSingleton<Domain.Analysis.IRoslynAnalyzer, Domain.Analysis.RoslynAnalyzer>();
        builder.Services.AddSingleton<Domain.Analysis.IDiscoveryService, Domain.Analysis.DiscoveryService>();

        // Register Termina and route
        builder.Services.AddTermina("/", termina =>
        {
            termina.RegisterRoute<UI.MainPage, UI.MainViewModel>("/");
        });

        var host = builder.Build();

        var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("ContextExtrator.CLI");
        logger.LogInformation("ContextExtrator CLI host starting with Termina.");

        await host.RunAsync();
    }
}
