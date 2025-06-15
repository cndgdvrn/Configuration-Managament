using DynamicConfig.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Sample.ServiceA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly IConfigurationReader _configReader;
    private readonly ILogger<TestController> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _serviceName;

    public TestController(IConfigurationReader configReader, ILogger<TestController> logger, IConfiguration configuration)
    {
        _configReader = configReader;
        _logger = logger;
        _configuration = configuration;
        
        // Environment variable'dan ApplicationName'i oku
        _serviceName = _configuration["ApplicationName"] ?? "UNKNOWN-SERVICE";
    }

    /// <summary>
    /// Tüm konfigürasyonları test eder ve döndürür
    /// </summary>
    [HttpGet]
    public ActionResult<object> GetAllConfigurations()
    {
        try
        {
            var configs = new
            {
                ServiceName = _serviceName,
                Timestamp = DateTime.UtcNow,
                Configurations = new
                {
                    // String konfigürasyon testi
                    SiteName = _configReader.GetValue<string>("SiteName") ?? "default-site.com",
                    
                    // Boolean konfigürasyon testi
                    IsBasketEnabled = _configReader.GetValue<bool>("IsBasketEnabled"),
                    
                    // Integer konfigürasyon testi
                    MaxItemCount = _configReader.GetValue<int>("MaxItemCount"),
                    
                    // Double konfigürasyon testi
                    DiscountRate = _configReader.GetValue<double>("DiscountRate"),
                    
                    // Olmayan konfigürasyon testi
                    NonExistentConfig = _configReader.GetValue<string>("NonExistentConfig")
                }
            };

            _logger.LogInformation("Konfigürasyonlar başarıyla okundu: {ServiceName}", _serviceName);
            return Ok(configs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Konfigürasyonlar okunurken hata oluştu: {ServiceName}", _serviceName);
            return StatusCode(500, "Konfigürasyonlar okunamadı");
        }
    }

    /// <summary>
    /// Belirli bir konfigürasyonu test eder
    /// </summary>
    [HttpGet("{key}")]
    public ActionResult<object> GetConfiguration(string key)
    {
        try
        {
            // Önce string olarak dene
            var stringValue = _configReader.GetValue<string>(key);
            
            var result = new
            {
                ServiceName = _serviceName, // Artık dinamik!
                Key = key,
                Value = stringValue,
                Timestamp = DateTime.UtcNow,
                Found = !string.IsNullOrEmpty(stringValue)
            };

            _logger.LogInformation("Konfigürasyon okundu: {ServiceName} - {Key} = {Value}", _serviceName, key, stringValue);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Konfigürasyon okunurken hata oluştu: {ServiceName} - {Key}", _serviceName, key);
            return StatusCode(500, $"Konfigürasyon okunamadı: {key}");
        }
    }

    /// <summary>
    /// Konfigürasyonları manuel olarak yeniler
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult> RefreshConfigurations()
    {
        try
        {
            await _configReader.RefreshAsync();
            
            _logger.LogInformation("Konfigürasyonlar manuel olarak yenilendi: {ServiceName}", _serviceName);
            return Ok(new { 
                ServiceName = _serviceName,
                Message = "Konfigürasyonlar başarıyla yenilendi", 
                Timestamp = DateTime.UtcNow 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Konfigürasyonlar yenilenirken hata oluştu: {ServiceName}", _serviceName);
            return StatusCode(500, "Konfigürasyonlar yenilenemedi");
        }
    }

    /// <summary>
    /// Servis durumu ve konfigürasyon bağlantı bilgilerini döndürür
    /// </summary>
    [HttpGet("health")]
    public ActionResult<object> GetHealth()
    {
        try
        {
            // Bir test konfigürasyonu okuyarak bağlantıyı kontrol et
            var testValue = _configReader.GetValue<string>("SiteName");
            
            var health = new
            {
                ServiceName = _serviceName, // Artık dinamik!
                Status = "Healthy",
                ConfigurationStatus = !string.IsNullOrEmpty(testValue) ? "Connected" : "Disconnected",
                Timestamp = DateTime.UtcNow,
                TestConfiguration = testValue
            };

            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check sırasında hata oluştu: {ServiceName}", _serviceName);
            
            return Ok(new
            {
                ServiceName = _serviceName,
                Status = "Unhealthy",
                ConfigurationStatus = "Error",
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }
} 