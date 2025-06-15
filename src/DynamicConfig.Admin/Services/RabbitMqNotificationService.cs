using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace DynamicConfig.Admin.Services;

public class RabbitMqNotificationService : INotificationService, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMqNotificationService> _logger;

    public RabbitMqNotificationService(IConfiguration configuration, ILogger<RabbitMqNotificationService> logger)
    {
        _logger = logger;

        try
        {
            var rabbitHost = configuration["RabbitMq:HostName"] ?? "localhost";
            var rabbitPort = configuration.GetValue<int>("RabbitMq:Port", 5672);
            var rabbitUsername = configuration["RabbitMq:Username"] ?? "guest";
            var rabbitPassword = configuration["RabbitMq:Password"] ?? "guest";

            var factory = new ConnectionFactory()
            {
                HostName = rabbitHost,
                Port = rabbitPort,
                UserName = rabbitUsername,
                Password = rabbitPassword
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(exchange: "config.updates", type: ExchangeType.Fanout);

            _logger.LogInformation("RabbitMQ bağlantısı kuruldu: {Host}:{Port}", rabbitHost, rabbitPort);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ bağlantısı kurulamadı");
            throw;
        }
    }

    public async Task NotifyConfigurationChangeAsync(string applicationName, string action)
    {
        try
        {
            var message = new
            {
                ApplicationName = applicationName,
                Action = action,
                Timestamp = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            _channel.BasicPublish(
                exchange: "config.updates",
                routingKey: "",
                basicProperties: null,
                body: body);

            _logger.LogInformation("Konfigürasyon değişikliği bildirimi gönderildi: {ApplicationName} - {Action}", 
                applicationName, action);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bildirim gönderilemedi: {ApplicationName} - {Action}", applicationName, action);
            throw;
        }
    }

    public async Task NotifyAllAsync(string action)
    {
        try
        {
            var message = new
            {
                ApplicationName = (string?)null, 
                Action = action,
                Timestamp = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            _channel.BasicPublish(
                exchange: "config.updates",
                routingKey: "",
                basicProperties: null,
                body: body);

            _logger.LogInformation("Genel bildirim gönderildi: {Action}", action);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Genel bildirim gönderilemedi: {Action}", action);
            throw;
        }
    }

    public void Dispose()
    {
        try
        {
            _channel?.Close();
            _channel?.Dispose();
            
            _connection?.Close();
            _connection?.Dispose();
            
            _logger.LogInformation("RabbitMQ bağlantısı kapatıldı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ bağlantısı kapatılırken hata oluştu");
        }
    }
} 