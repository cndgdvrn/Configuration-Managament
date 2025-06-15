using DynamicConfig.Core.Interfaces;
using DynamicConfig.Core.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace DynamicConfig.Core.Repositories;

public class MongoConfigurationRepository : IConfigurationRepository
{
    private readonly IMongoCollection<ConfigurationItem> _configurations;
    private readonly ILogger<MongoConfigurationRepository> _logger;

    public MongoConfigurationRepository(string connectionString, ILogger<MongoConfigurationRepository> logger)
    {
        _logger = logger;
        
        try
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase("DynamicConfig");
            _configurations = database.GetCollection<ConfigurationItem>("configurations");

            // Index oluştur
            CreateIndexes();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MongoDB bağlantısı kurulamadı: {ConnectionString}", connectionString);
            throw;
        }
    }

    private void CreateIndexes()
    {
        try
        {
            // ApplicationName için index
            var applicationNameIndex = Builders<ConfigurationItem>.IndexKeys.Ascending(x => x.ApplicationName);
            _configurations.Indexes.CreateOne(new CreateIndexModel<ConfigurationItem>(applicationNameIndex));

            // Name için index
            var nameIndex = Builders<ConfigurationItem>.IndexKeys.Ascending(x => x.Name);
            _configurations.Indexes.CreateOne(new CreateIndexModel<ConfigurationItem>(nameIndex));

            // ApplicationName + Name için index
            var compositeIndex = Builders<ConfigurationItem>.IndexKeys
                .Ascending(x => x.ApplicationName)
                .Ascending(x => x.Name);
            _configurations.Indexes.CreateOne(new CreateIndexModel<ConfigurationItem>(compositeIndex));

            _logger.LogInformation("MongoDB indexleri oluşturuldu");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MongoDB index oluşturulurken hata oluştu");
        }
    }

    public async Task<List<ConfigurationItem>> GetActiveByApplicationNameAsync(string applicationName)
    {
        try
        {
            var filter = Builders<ConfigurationItem>.Filter.And(
                Builders<ConfigurationItem>.Filter.Eq(x => x.ApplicationName, applicationName),
                Builders<ConfigurationItem>.Filter.Eq(x => x.IsActive, true)
            );

            var result = await _configurations.Find(filter).ToListAsync();
            _logger.LogDebug("Uygulama {ApplicationName} için {Count} aktif konfigürasyon bulundu", 
                applicationName, result.Count);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Uygulama {ApplicationName} için konfigürasyonlar alınamadı", applicationName);
            return new List<ConfigurationItem>();
        }
    }

    public async Task<List<ConfigurationItem>> GetAllActiveAsync()
    {
        try
        {
            var filter = Builders<ConfigurationItem>.Filter.Eq(x => x.IsActive, true);
            var result = await _configurations.Find(filter).ToListAsync();
            
            _logger.LogDebug("{Count} aktif konfigürasyon bulundu", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tüm aktif konfigürasyonlar alınamadı");
            return new List<ConfigurationItem>();
        }
    }

    public async Task<ConfigurationItem?> GetByIdAsync(string id)
    {
        try
        {
            var filter = Builders<ConfigurationItem>.Filter.Eq(x => x.Id, id);
            var result = await _configurations.Find(filter).FirstOrDefaultAsync();
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ID {Id} ile konfigürasyon alınamadı", id);
            return null;
        }
    }

    public async Task<ConfigurationItem> AddAsync(ConfigurationItem item)
    {
        try
        {
            item.LastUpdatedAt = DateTime.UtcNow;
            await _configurations.InsertOneAsync(item);
            
            _logger.LogInformation("Yeni konfigürasyon eklendi: {ApplicationName}.{Name}", 
                item.ApplicationName, item.Name);
            
            return item;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Konfigürasyon eklenemedi: {ApplicationName}.{Name}", 
                item.ApplicationName, item.Name);
            throw;
        }
    }

    public async Task<bool> UpdateAsync(ConfigurationItem item)
    {
        try
        {
            item.LastUpdatedAt = DateTime.UtcNow;
            var filter = Builders<ConfigurationItem>.Filter.Eq(x => x.Id, item.Id);
            var result = await _configurations.ReplaceOneAsync(filter, item);
            
            var success = result.ModifiedCount > 0;
            if (success)
            {
                _logger.LogInformation("Konfigürasyon güncellendi: {ApplicationName}.{Name}", 
                    item.ApplicationName, item.Name);
            }
            else
            {
                _logger.LogWarning("Konfigürasyon güncellenemedi: {Id}", item.Id);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Konfigürasyon güncellenemedi: {Id}", item.Id);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(string id)
    {
        try
        {
            var filter = Builders<ConfigurationItem>.Filter.Eq(x => x.Id, id);
            var result = await _configurations.DeleteOneAsync(filter);
            
            var success = result.DeletedCount > 0;
            if (success)
            {
                _logger.LogInformation("Konfigürasyon silindi: {Id}", id);
            }
            else
            {
                _logger.LogWarning("Konfigürasyon silinemedi: {Id}", id);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Konfigürasyon silinemedi: {Id}", id);
            return false;
        }
    }

    public async Task<List<ConfigurationItem>> GetUpdatedSinceAsync(string applicationName, DateTime since)
    {
        try
        {
            var filter = Builders<ConfigurationItem>.Filter.And(
                Builders<ConfigurationItem>.Filter.Eq(x => x.ApplicationName, applicationName),
                Builders<ConfigurationItem>.Filter.Eq(x => x.IsActive, true),
                Builders<ConfigurationItem>.Filter.Gt(x => x.LastUpdatedAt, since)
            );

            var result = await _configurations.Find(filter).ToListAsync();
            
            _logger.LogDebug("Uygulama {ApplicationName} için {Since} tarihinden sonra {Count} güncelleme bulundu", 
                applicationName, since, result.Count);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Uygulama {ApplicationName} için güncelleme kontrolü yapılamadı", applicationName);
            return new List<ConfigurationItem>();
        }
    }
} 