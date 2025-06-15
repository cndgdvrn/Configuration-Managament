namespace DynamicConfig.Core.Interfaces;

/// <summary>
/// Dinamik konfigürasyon okuma işlemleri için ana arayüz
/// </summary>
public interface IConfigurationReader : IDisposable
{
    /// <summary>
    /// Belirtilen anahtar ile konfigürasyon değerini belirtilen tipte getirir
    /// </summary>
    T GetValue<T>(string key);

    /// <summary>
    /// Async olarak konfigürasyon değerini getirir
    /// </summary>
    Task<T> GetValueAsync<T>(string key);

    /// <summary>
    /// Konfigürasyonların manuel olarak yenilenmesini sağlar
    /// </summary>
    Task RefreshAsync();
} 