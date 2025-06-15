using DynamicConfig.Core.Interfaces;
using DynamicConfig.Core.Repositories;
using DynamicConfig.Admin.Services;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Controller leri ekle
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS ayarları
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

// MongoDB Repository 
builder.Services.AddScoped<IConfigurationRepository>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var logger = serviceProvider.GetRequiredService<ILogger<MongoConfigurationRepository>>();
    
    var connectionString = configuration.GetConnectionString("MongoDb")
        ?? throw new InvalidOperationException("MongoDB connection string is missing");
    
    return new MongoConfigurationRepository(connectionString, logger);
});

// RabbitMQ Notification Service 
builder.Services.AddScoped<INotificationService, RabbitMqNotificationService>();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

// Statik dosyalar için wwwroot klasörünü dışarı aç
app.UseStaticFiles();

// Ana sayfa yönlendirmesi
app.MapGet("/", () => Results.Redirect("/index.html"));

app.Run(); 