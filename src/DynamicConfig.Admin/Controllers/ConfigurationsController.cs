using DynamicConfig.Core.Interfaces;
using DynamicConfig.Core.Models;
using DynamicConfig.Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace DynamicConfig.Admin.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigurationsController : ControllerBase
{
    private readonly IConfigurationRepository _repository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ConfigurationsController> _logger;

    public ConfigurationsController(
        IConfigurationRepository repository,
        INotificationService notificationService,
        ILogger<ConfigurationsController> logger)
    {
        _repository = repository;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Tüm aktif konfigürasyonları listeler
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ConfigurationItem>>> GetAll([FromQuery] string? name = null)
    {
        try
        {
            var configurations = await _repository.GetAllActiveAsync();
            
            // İsim filtresi varsa uygula
            if (!string.IsNullOrWhiteSpace(name))
            {
                configurations = configurations
                    .Where(c => c.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            _logger.LogInformation("Konfigürasyonlar listelendi. Toplam: {Count}, Filtre: {Filter}", 
                configurations.Count, name ?? "Yok");

            return Ok(configurations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Konfigürasyonlar listelenirken hata oluştu");
            return StatusCode(500, "Konfigürasyonlar alınamadı");
        }
    }

    /// <summary>
    /// ID'ye göre konfigürasyon getirir
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ConfigurationItem>> GetById(string id)
    {
        try
        {
            var configuration = await _repository.GetByIdAsync(id);
            
            if (configuration == null)
            {
                _logger.LogWarning("Konfigürasyon bulunamadı: {Id}", id);
                return NotFound($"ID {id} ile konfigürasyon bulunamadı");
            }

            return Ok(configuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Konfigürasyon getirilirken hata oluştu: {Id}", id);
            return StatusCode(500, "Konfigürasyon alınamadı");
        }
    }

    /// <summary>
    /// Belirli bir uygulama için konfigürasyonları getirir
    /// </summary>
    [HttpGet("application/{applicationName}")]
    public async Task<ActionResult<List<ConfigurationItem>>> GetByApplication(string applicationName)
    {
        try
        {
            var configurations = await _repository.GetActiveByApplicationNameAsync(applicationName);
            
            _logger.LogInformation("Uygulama konfigürasyonları getirildi: {ApplicationName}, Toplam: {Count}", 
                applicationName, configurations.Count);

            return Ok(configurations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Uygulama konfigürasyonları getirilirken hata oluştu: {ApplicationName}", 
                applicationName);
            return StatusCode(500, "Uygulama konfigürasyonları alınamadı");
        }
    }

    /// <summary>
    /// Yeni konfigürasyon ekler
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ConfigurationItem>> Create([FromBody] CreateConfigurationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var item = new ConfigurationItem
            {
                Name = request.Name,
                Type = request.Type,
                Value = request.Value,
                IsActive = request.IsActive,
                ApplicationName = request.ApplicationName,
                LastUpdatedAt = DateTime.UtcNow
            };

            var created = await _repository.AddAsync(item);
            
            // RabbitMQ ile bildirim gönder
            await _notificationService.NotifyConfigurationChangeAsync(created.ApplicationName, "Created");
            
            _logger.LogInformation("Yeni konfigürasyon eklendi: {ApplicationName}.{Name}", 
                created.ApplicationName, created.Name);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Konfigürasyon eklenirken hata oluştu: {ApplicationName}.{Name}", 
                request.ApplicationName, request.Name);
            return StatusCode(500, "Konfigürasyon eklenemedi");
        }
    }

    /// <summary>
    /// Mevcut konfigürasyonu günceller
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ConfigurationItem>> Update(string id, [FromBody] UpdateConfigurationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound($"ID {id} ile konfigürasyon bulunamadı");
            }

            existing.Name = request.Name;
            existing.Type = request.Type;
            existing.Value = request.Value;
            existing.IsActive = request.IsActive;
            existing.ApplicationName = request.ApplicationName;
            existing.LastUpdatedAt = DateTime.UtcNow;

            var success = await _repository.UpdateAsync(existing);
            
            if (!success)
            {
                return StatusCode(500, "Konfigürasyon güncellenemedi");
            }

            await _notificationService.NotifyConfigurationChangeAsync(existing.ApplicationName, "Updated");
            
            _logger.LogInformation("Konfigürasyon güncellendi: {ApplicationName}.{Name}", 
                existing.ApplicationName, existing.Name);

            return Ok(existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Konfigürasyon güncellenirken hata oluştu: {Id}", id);
            return StatusCode(500, "Konfigürasyon güncellenemedi");
        }
    }

    /// <summary>
    /// Konfigürasyonu siler
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound($"ID {id} ile konfigürasyon bulunamadı");
            }

            var success = await _repository.DeleteAsync(id);
            
            if (!success)
            {
                return StatusCode(500, "Konfigürasyon silinemedi");
            }

            await _notificationService.NotifyConfigurationChangeAsync(existing.ApplicationName, "Deleted");
            
            _logger.LogInformation("Konfigürasyon silindi: {ApplicationName}.{Name}", 
                existing.ApplicationName, existing.Name);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Konfigürasyon silinirken hata oluştu: {Id}", id);
            return StatusCode(500, "Konfigürasyon silinemedi");
        }
    }
}

// DTO lar
public class CreateConfigurationRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string ApplicationName { get; set; } = string.Empty;
}

public class UpdateConfigurationRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string ApplicationName { get; set; } = string.Empty;
} 