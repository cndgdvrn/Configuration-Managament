using DynamicConfig.Core.Models;

namespace DynamicConfig.Core.Interfaces;

/// <summary>
/// Konfigürasyon verilerine erişim için repository arayüzü
/// </summary>
public interface IConfigurationRepository
{
    /// <summary>
    /// Belirtilen uygulama adına ait aktif konfigürasyonları getirir
    /// </summary>
    Task<List<ConfigurationItem>> GetActiveByApplicationNameAsync(string applicationName);

    /// <summary>
    /// Tüm aktif konfigürasyonları getirir
    Task<List<ConfigurationItem>> GetAllActiveAsync();

    /// <summary>
    /// ID'ye göre konfigürasyon getirir
    /// </summary>
    Task<ConfigurationItem?> GetByIdAsync(string id);

    /// <summary>
    /// Yeni konfigürasyon ekler
    /// </summary>
    Task<ConfigurationItem> AddAsync(ConfigurationItem item);

    /// <summary>
    /// Konfigürasyonu günceller
    /// </summary>
    Task<bool> UpdateAsync(ConfigurationItem item);

    /// <summary>
    /// Konfigürasyonu siler
    /// </summary>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// Son güncelleme zamanına göre filtreler
    /// </summary>
    Task<List<ConfigurationItem>> GetUpdatedSinceAsync(string applicationName, DateTime since);
} 