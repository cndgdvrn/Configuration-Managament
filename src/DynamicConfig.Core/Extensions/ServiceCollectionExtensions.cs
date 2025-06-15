using DynamicConfig.Core.Interfaces;
using DynamicConfig.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DynamicConfig.Core.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// DynamicConfig.Core servislerini DI container'a ekler
    /// </summary>
    public static IServiceCollection AddDynamicConfiguration(
        this IServiceCollection services,
        string applicationName,
        string connectionString,
        int refreshTimerIntervalInMs = 300000) // Default 5 dakika
    {
        if (string.IsNullOrWhiteSpace(applicationName))
            throw new ArgumentException("Application name cannot be null or empty", nameof(applicationName));
        
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

        // ConfigurationReader'ı singleton olarak kaydet
        services.AddSingleton<IConfigurationReader>(serviceProvider =>
        {
            var logger = serviceProvider.GetService<ILogger<ConfigurationReader>>();
            return new ConfigurationReader(applicationName, connectionString, refreshTimerIntervalInMs, logger);
        });

        return services;
    }

    /// <summary>
    /// Configuration'dan parametreleri okuyarak DynamicConfig.Core servislerini ekler
    /// </summary>

    public static IServiceCollection AddDynamicConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var applicationName = configuration["ApplicationName"] 
            ?? configuration["DynamicConfig:ApplicationName"]
            ?? throw new InvalidOperationException("ApplicationName configuration is missing");

        var connectionString = configuration.GetConnectionString("MongoDb")
            ?? configuration.GetConnectionString("DynamicConfig")
            ?? throw new InvalidOperationException("MongoDB connection string is missing");

        var refreshInterval = configuration.GetValue<int?>("ConfigRefreshIntervalMs") 
            ?? configuration.GetValue<int?>("DynamicConfig:RefreshIntervalMs") 
            ?? 300000; // Varsayılan olarak 5 dakika

        return services.AddDynamicConfiguration(applicationName, connectionString, refreshInterval);
    }
} 