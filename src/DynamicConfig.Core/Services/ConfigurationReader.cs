using DynamicConfig.Core.Interfaces;
using DynamicConfig.Core.Models;
using DynamicConfig.Core.Repositories;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace DynamicConfig.Core.Services;

public class ConfigurationReader : IConfigurationReader
{
    private readonly string _applicationName;
    private readonly string _connectionString;
    private readonly int _refreshTimerIntervalInMs;
    private readonly IConfigurationRepository _repository;
    private readonly ILogger<ConfigurationReader> _logger;

    // Thread-safe cache için ConcurrentDictionary == concurrent hashmap
    private ConcurrentDictionary<string, object> _cache = new();
    
    // Concurrency kontrolü için SemaphoreSlim == semaphore
    private readonly SemaphoreSlim _updateLock = new(1, 1);
    
    //Timer == @scheduled yada TaskExecutor  
    private readonly Timer _refreshTimer;
    
    // Son başarılı güncelleme zamanı
    private DateTime _lastSuccessfulUpdate = DateTime.MinValue;
    
    private IConnection? _rabbitConnection;
    private IModel? _rabbitChannel;
    private string? _queueName;

    // Disposed flag == bean lifecycle gibi, instance'ın lifecycle'i bitince set edilir
    private bool _disposed = false;

    public ConfigurationReader(string applicationName, string connectionString, 
        int refreshTimerIntervalInMs, ILogger<ConfigurationReader>? logger = null)
    {
        _applicationName = applicationName ?? throw new ArgumentNullException(nameof(applicationName));
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _refreshTimerIntervalInMs = refreshTimerIntervalInMs;
        
        _logger = logger ?? CreateConsoleLogger();
        
        _repository = new MongoConfigurationRepository(_connectionString, 
            CreateLogger<MongoConfigurationRepository>());

        _logger.LogInformation("ConfigurationReader başlatılıyor: {ApplicationName}", _applicationName);

        // İlk yüklemeyi senkron olarak yapılır
        try
        {
            LoadConfigurationsAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İlk konfigürasyon yüklemesi başarısız oldu");
        }

        // Periyodik güncelleme timer'ını başlar
        _refreshTimer = new Timer(async _ => await RefreshConfigurationsAsync(), 
            null, _refreshTimerIntervalInMs, _refreshTimerIntervalInMs);

        try
        {
            InitializeRabbitMqListener();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RabbitMQ bağlantısı kurulamadı, sadece polling mekanizması kullanılacak");
        }
    }

    public T GetValue<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            _logger.LogWarning("GetValue çağrısında boş key kullanıldı");
            return default(T)!;
        }

        if (_cache.TryGetValue(key, out var value))
        {
            try
            {
                if (value is T directValue)
                {
                    return directValue;
                }

                // Tip dönüşümü yapılır
                var convertedValue = ConvertValue<T>(value);
                _logger.LogDebug("Konfigürasyon değeri okundu: {Key} = {Value}", key, convertedValue);
                return convertedValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Konfigürasyon değeri dönüştürülemedi: {Key}, Değer: {Value}, Hedef Tip: {Type}", 
                    key, value, typeof(T).Name);
                return default(T)!;
            }
        }

        _logger.LogWarning("Konfigürasyon anahtarı bulunamadı: {Key}", key);
        return default(T)!;
    }

    public Task<T> GetValueAsync<T>(string key)
    {

        return Task.FromResult(GetValue<T>(key));
    }

    public async Task RefreshAsync()
    {
        await RefreshConfigurationsAsync();
    }

    private async Task RefreshConfigurationsAsync()
    {
        // Semaphore'a lock atar
        await _updateLock.WaitAsync();
        try
        {
            await LoadConfigurationsAsync();
        }
        finally
        {
            _updateLock.Release();
        }
    }

    private async Task LoadConfigurationsAsync()
    {
        try
        {
            var configs = await _repository.GetActiveByApplicationNameAsync(_applicationName);
            
            if (configs.Count == 0)
            {
                _logger.LogWarning("Uygulama {ApplicationName} için hiç aktif konfigürasyon bulunamadı", 
                    _applicationName);
                return;
            }

            var newCache = new ConcurrentDictionary<string, object>();
            
            foreach (var config in configs)
            {
                try
                {
                    // Değeri doğru tipe çevir
                    var typedValue = ConvertStringToType(config.Value, config.Type);
                    newCache.TryAdd(config.Name, typedValue);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Konfigürasyon değeri dönüştürülemedi: {Name} = {Value} ({Type})", 
                        config.Name, config.Value, config.Type);
                }
            }

            // Atomik olarak eski cache'i yenisiyle değiştirir
            _cache = newCache;
            _lastSuccessfulUpdate = DateTime.UtcNow;
            
            _logger.LogInformation("Konfigürasyon başarıyla güncellendi: {ApplicationName}, {Count} kayıt", 
                _applicationName, newCache.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Konfigürasyon yüklenemedi: {ApplicationName}. Son başarılı güncelleme: {LastUpdate}", 
                _applicationName, _lastSuccessfulUpdate);
        }
    }

    private void InitializeRabbitMqListener()
    {
        try
        {
            // RabbitMQ connection string'den host bilgisini çıkarir
            var rabbitHost = ExtractRabbitHostFromConnectionString();
            
            var factory = new ConnectionFactory() { HostName = rabbitHost };
            _rabbitConnection = factory.CreateConnection();
            _rabbitChannel = _rabbitConnection.CreateModel();

            _rabbitChannel.ExchangeDeclare(exchange: "config.updates", type: ExchangeType.Fanout);

            _queueName = _rabbitChannel.QueueDeclare().QueueName;
            _rabbitChannel.QueueBind(queue: _queueName, exchange: "config.updates", routingKey: "");

            var consumer = new EventingBasicConsumer(_rabbitChannel);
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    
                    var updateMessage = JsonSerializer.Deserialize<ConfigUpdateMessage>(message);
                    
                    // Bu uygulama için bir güncelleme mi?
                    if (updateMessage?.ApplicationName == _applicationName || 
                        string.IsNullOrEmpty(updateMessage?.ApplicationName))
                    {
                        _logger.LogInformation("RabbitMQ'dan güncelleme mesajı alındı: {ApplicationName}", 
                            _applicationName);
                        await RefreshConfigurationsAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "RabbitMQ mesajı işlenirken hata oluştu");
                }
            };

            _rabbitChannel.BasicConsume(queue: _queueName, autoAck: true, consumer: consumer);
            
            _logger.LogInformation("RabbitMQ dinleyicisi başlatıldı: {ApplicationName}", _applicationName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RabbitMQ bağlantısı kurulamadı");
            throw;
        }
    }

    private string ExtractRabbitHostFromConnectionString()
    {
        // MongoDB connection string'den RabbitMQ host'u çıkar
        if (_connectionString.Contains("localhost"))
            return "localhost";
        
        // Docker ortamında
        return "rabbitmq";
    }

    private static object ConvertStringToType(string value, string type)
    {
        return type.ToLowerInvariant() switch
        {
            "int" or "integer" => int.Parse(value),
            "bool" or "boolean" => bool.Parse(value),
            "double" => double.Parse(value),
            "float" => float.Parse(value),
            "decimal" => decimal.Parse(value),
            "long" => long.Parse(value),
            _ => value
        };
    }

    private static T ConvertValue<T>(object value)
    {
        if (value is T directValue)
            return directValue;

        if (typeof(T) == typeof(string))
            return (T)(object)value.ToString()!;

        return (T)Convert.ChangeType(value, typeof(T));
    }

    private ILogger<ConfigurationReader> CreateConsoleLogger()
    {
        using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddConsole());
        return loggerFactory.CreateLogger<ConfigurationReader>();
    }

    private ILogger<T> CreateLogger<T>()
    {
        using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddConsole());
        return loggerFactory.CreateLogger<T>();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            _refreshTimer?.Dispose();
            _updateLock?.Dispose();
            
            _rabbitChannel?.Close();
            _rabbitChannel?.Dispose();
            
            _rabbitConnection?.Close();
            _rabbitConnection?.Dispose();
            
            _logger.LogInformation("ConfigurationReader dispose edildi: {ApplicationName}", _applicationName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ConfigurationReader dispose edilirken hata oluştu");
        }
    }

    // RabbitMQ event DTO
    private class ConfigUpdateMessage
    {
        public string? ApplicationName { get; set; }
        public string? Action { get; set; }
    }
} 