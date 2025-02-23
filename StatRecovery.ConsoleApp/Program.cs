using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StatRecovery.Application.Interfaces;
using StatRecovery.Application.UseCases;
using StatRecovery.Core.Interfaces;
using StatRecovery.Core.Services;
using StatRecovery.Infrastructure.Interfaces;
using StatRecovery.Infrastructure.Services;

class Program
{
    static async Task Main(string[] args)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var serviceProvider = ConfigureServices(configuration);
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            var processFilesUseCase = services.GetRequiredService<IProcessFilesUseCase>();
            await processFilesUseCase.ExecuteAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while processing files.");
        }
    }
    
    private static IServiceProvider ConfigureServices(IConfiguration configuration)
    {
        var services = new ServiceCollection();

        services.AddSingleton(configuration);
        
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddConsole();
            loggingBuilder.SetMinimumLevel(LogLevel.Information);
        });
        
        services.AddSingleton<IS3StorageService, S3StorageService>();
        services.AddSingleton<ICsvParserService, CsvParserService>();
        services.AddSingleton<IZipService, ZipService>();
        services.AddSingleton<IMetadataService, MetadataService>();
        services.AddSingleton<IProcessFilesUseCase, ProcessFilesUseCase>();

        return services.BuildServiceProvider();
    }
}
