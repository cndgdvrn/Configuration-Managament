using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DynamicConfig.Core.Models;

public class ConfigurationItem
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("Name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("Type")]
    public string Type { get; set; } = string.Empty;

    [BsonElement("Value")]
    public string Value { get; set; } = string.Empty;

    [BsonElement("IsActive")]
    public bool IsActive { get; set; }

    [BsonElement("ApplicationName")]
    public string ApplicationName { get; set; } = string.Empty;

    [BsonElement("LastUpdatedAt")]
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
} 