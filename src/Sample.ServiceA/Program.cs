using DynamicConfig.Core.Extensions;
using DynamicConfig.Core.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Controllers ekle
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
// Swagger'da özel bir title belirle
builder.Services.AddSwaggerGen(c =>
{
    var serviceName = builder.Configuration["ApplicationName"] ?? "Unknown Service";
    c.SwaggerDoc("v1", new() { Title = $"{serviceName} API", Version = "v1" });
});

// DynamicConfig.Core kütüphanesini ekle
// Bu satır runtime'da yeni servis eklenmesinin anahtarı!
// Her servis kendi ApplicationName'i ile başlar ve sadece kendi konfigürasyonlarını alır
builder.Services.AddDynamicConfiguration(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Konfigürasyon test endpoint'i
app.MapGet("/", (IConfiguration config) => 
{
    var serviceName = config["ApplicationName"] ?? "Unknown Service";
    var otherService = serviceName == "SERVICE-A" ? "SERVICE-B" : "SERVICE-A";
    var otherPort = serviceName == "SERVICE-A" ? "8082" : "8081";
    
    var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Dynamic Configuration {serviceName}</title>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 40px; background: #f5f5f5; }}
        .container {{ background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .status {{ color: #28a745; font-weight: bold; font-size: 18px; }}
        .endpoint {{ background: #e9ecef; padding: 15px; margin: 10px 0; border-radius: 4px; font-family: monospace; }}
        .endpoint a {{ color: #007bff; text-decoration: none; }}
        .endpoint a:hover {{ text-decoration: underline; }}
        h1 {{ color: #333; }}
        h2 {{ color: #666; margin-top: 30px; }}
        .service-badge {{ background: #007bff; color: white; padding: 4px 8px; border-radius: 4px; font-size: 14px; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>🚀 Dynamic Configuration <span class='service-badge'>{serviceName}</span></h1>
        <p class='status'>✅ Servis calisiyor!</p>
        
        <h2>📋 Test Endpoints:</h2>
        <div class='endpoint'>
            <strong>Tum Konfigurasyonlar:</strong><br>
            <a href='/api/test' target='_blank'>GET /api/test</a>
        </div>
        
        <div class='endpoint'>
            <strong>Swagger UI:</strong><br>
            <a href='/swagger' target='_blank'>GET /swagger</a>
        </div>
        
        <div class='endpoint'>
            <strong>Saglik Kontrolu:</strong><br>
            <a href='/api/test/health' target='_blank'>GET /api/test/health</a>
        </div>
        
        <div class='endpoint'>
            <strong>Belirli Konfigurasyon:</strong><br>
            <a href='/api/test/SiteName' target='_blank'>GET /api/test/SiteName</a>
        </div>
        
        <h2>🎛️ Admin Panel:</h2>
        <div class='endpoint'>
            <strong>Konfigurasyon Yonetimi:</strong><br>
            <a href='http://localhost:8080' target='_blank'>http://localhost:8080</a>
        </div>
        
        <h2>🔄 Diger Servisler:</h2>
        <div class='endpoint'>
            <strong>{otherService}:</strong><br>
            <a href='http://localhost:{otherPort}' target='_blank'>http://localhost:{otherPort}</a>
        </div>
    </div>
</body>
</html>";
    
    return Results.Content(html, "text/html; charset=utf-8");
});

app.Run(); 