# Dynamic Configuration System

Bu proje, .NET 8 tabanlı dinamik konfigürasyon yönetim sistemidir. Uygulamaların restart/recycle gerektirmeden runtime'da konfigürasyon değişikliklerini alabilmesini sağlar.

## Mimari

### Core Bileşenler

- **DynamicConfig.Core**: Ana kütüphane (.NET 8 Class Library)
- **DynamicConfig.Admin**: Web yönetim paneli (ASP.NET Core Web API)
- **Sample.ServiceA**: Örnek servis implementasyonu
- **MongoDB**: Konfigürasyon veritabanı
- **RabbitMQ**: Gerçek zamanlı bildirim sistemi

### Teknoloji Stack

- **.NET 8**: Ana framework
- **MongoDB**: NoSQL veritabanı
- **RabbitMQ**: Message broker
- **Docker & Docker Compose**: Containerization
- **Bootstrap**: Admin panel UI

## Hızlı Başlangıç

### Gereksinimler

- Docker Desktop
- .NET 8 SDK (geliştirme için)

### Kurulum

1. **Projeyi klonlayın:**
```bash
git clone https://github.com/cndgdvrn/Configuration-Managament
cd secil-case-c#-v2
```

2. **Docker ile çalıştırın:**
```bash
docker-compose up -d
```

### Hızlı Test

1. **Ana sayfa kontrolü:** http://localhost:8081
2. **API testi:** Sayfadaki "Tüm Konfigürasyonlar" linkine tıklayın
3. **Admin panel:** http://localhost:8080 - yeni konfigürasyon ekleyin
4. **Real-time test:** Admin'de değişiklik yapın, SERVICE-A endpoint'ini tekrar çağırın

3. **Servislere erişin:**
- Admin Panel: http://localhost:8080
- SERVICE-A: http://localhost:8081
- SERVICE-B: http://localhost:8082
- MongoDB: localhost:27017 -- username = admin / password = password123
- RabbitMQ Management: http://localhost:15672


**API Endpoint'leri:**
- `GET /api/test` - Tüm konfigürasyonları gösterir
- `GET /api/test/health` - Servis durumu
- `GET /api/test/{key}` - Belirli konfigürasyon değeri
- `POST /api/test/refresh` - Manuel konfigürasyon yenileme





## Özellikler

### Temel Özellikler

- **Multi-tenant**: Her servis sadece kendi konfigürasyonlarını görür
- **Type-safe**: Generic method ile tip güvenliği
- **Real-time**: RabbitMQ ile anında bildirimler
- **Resilient**: MongoDB erişilemezse cache'den çalışır
- **Thread-safe**: Concurrent erişim desteği
- **Auto-refresh**: Belirli aralıklarla otomatik yenileme

### Admin Panel

- Konfigürasyon CRUD işlemleri
- Uygulama bazında filtreleme
- İsim bazında arama
- Real-time güncellemeler



## Konfigürasyon Modeli

| Alan | Tip | Açıklama |
|------|-----|----------|
| Id | string | Unique identifier |
| Name | string | Konfigürasyon anahtarı |
| Type | string | Veri tipi (string, int, bool, double) |
| Value | string | Konfigürasyon değeri |
| IsActive | bool | Aktiflik durumu |
| ApplicationName | string | Hangi servise ait |
| LastUpdatedAt | DateTime | Son güncelleme zamanı |

## Test

```bash
# Testleri çalıştır
dotnet test
```

## Performance

- **Cache**: In-memory ConcurrentDictionary
- **Connection Pooling**: MongoDB driver optimizasyonu
- **Async/Await**: Non-blocking operations
- **Semaphore**: Thread-safe cache updates
- **Background Services**: Timer-based refresh

## Monitoring

### Health Checks

```bash
# Servis durumu
GET /api/test/health

# Konfigürasyon testi
GET /api/test

# Manuel refresh
POST /api/test/refresh
```

## Runtime Servis Ekleme

Sistem çalışırken yeni servis eklemek için:

1. Yeni servis container'ını başlatın
2. Farklı `ApplicationName` environment variable'ı verin
3. Admin panel'den yeni servise özel konfigürasyonlar ekleyin
4. Servis otomatik olarak kendi konfigürasyonlarını çekmeye başlar

## API Endpoints

### Admin API (Port 8080)

- `GET /api/Configurations` - Tüm konfigürasyonları listele
- `GET /api/Configurations/{id}` - ID ile konfigürasyon getir
- `GET /api/Configurations/application/{name}` - Uygulama konfigürasyonları
- `POST /api/Configurations` - Yeni konfigürasyon ekle
- `PUT /api/Configurations/{id}` - Konfigürasyon güncelle
- `DELETE /api/Configurations/{id}` - Konfigürasyon sil

### Service API (Port 8081, 8082)

- `GET /api/test` - Tüm konfigürasyonları test et
- `GET /api/test/{key}` - Belirli konfigürasyonu test et
- `GET /api/test/health` - Servis durumu
- `POST /api/test/refresh` - Manuel yenileme

---