namespace DynamicConfig.Admin.Services;

/// <summary>
/// Konfigürasyon değişikliklerini bildirim için servis arayüzü
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Konfigürasyon değişikliğini tüm ilgili servislere bildirir
    /// </summary>
    Task NotifyConfigurationChangeAsync(string applicationName, string action);

    /// <summary>
    /// Tüm servislere genel bildirim gönderir
    /// </summary>
    Task NotifyAllAsync(string action);
} 