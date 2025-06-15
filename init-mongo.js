db = db.getSiblingDB('DynamicConfig');
db.configurations.insertMany([
  {
    "Name": "SiteName",
    "Type": "string",
    "Value": "soty.io",
    "IsActive": true,
    "ApplicationName": "SERVICE-A",
    "LastUpdatedAt": new Date()
  },
  {
    "Name": "IsBasketEnabled",
    "Type": "bool",
    "Value": "true",
    "IsActive": true,
    "ApplicationName": "SERVICE-A",
    "LastUpdatedAt": new Date()
  },
  {
    "Name": "MaxItemCount",
    "Type": "int",
    "Value": "50",
    "IsActive": false,  
    "ApplicationName": "SERVICE-A",
    "LastUpdatedAt": new Date()
  },
  {
    "Name": "DiscountRate",
    "Type": "double",
    "Value": "0.15",
    "IsActive": true,
    "ApplicationName": "SERVICE-A",
    "LastUpdatedAt": new Date()
  },
  // SERVICE-B için örnek konfigürasyonlar
  {
    "Name": "DatabaseTimeout",
    "Type": "int",
    "Value": "30",
    "IsActive": true,
    "ApplicationName": "SERVICE-B",
    "LastUpdatedAt": new Date()
  },
  {
    "Name": "EnableLogging",
    "Type": "bool",
    "Value": "true",
    "IsActive": true,
    "ApplicationName": "SERVICE-B",
    "LastUpdatedAt": new Date()
  },
  {
    "Name": "ApiKey",
    "Type": "string",
    "Value": "sk-1234567890abcdef",
    "IsActive": true,
    "ApplicationName": "SERVICE-B",
    "LastUpdatedAt": new Date()
  },
  // SERVICE-C için örnek konfigürasyonlar (runtime'da eklenecek servis)
  {
    "Name": "NewFeatureToggle",
    "Type": "bool",
    "Value": "true",
    "IsActive": true,
    "ApplicationName": "SERVICE-C",
    "LastUpdatedAt": new Date()
  },
  {
    "Name": "TimeoutSeconds",
    "Type": "int",
    "Value": "60",
    "IsActive": true,
    "ApplicationName": "SERVICE-C",
    "LastUpdatedAt": new Date()
  }
]);

// Index'leri oluştur (performans için)
db.configurations.createIndex({ "ApplicationName": 1 });
db.configurations.createIndex({ "Name": 1 });
db.configurations.createIndex({ "ApplicationName": 1, "Name": 1 }, { unique: true });
db.configurations.createIndex({ "IsActive": 1 });
db.configurations.createIndex({ "LastUpdatedAt": 1 });

print('MongoDB initialization completed successfully!');
print('Total configurations inserted: ' + db.configurations.countDocuments());
print('SERVICE-A configurations: ' + db.configurations.countDocuments({"ApplicationName": "SERVICE-A"}));
print('SERVICE-B configurations: ' + db.configurations.countDocuments({"ApplicationName": "SERVICE-B"}));
print('SERVICE-C configurations: ' + db.configurations.countDocuments({"ApplicationName": "SERVICE-C"})); 