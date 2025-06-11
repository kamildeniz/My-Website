# Kişisel Web Sitesi

Bu proje, ASP.NET Core 8 ve Razor Pages kullanılarak oluşturulmuş kişisel bir portföy web sitesidir. Site, blog yazıları, projeler, hakkımda ve iletişim bölümlerinden oluşmaktadır.

## Özellikler

- **Blog Sistemi**: Markdown tabanlı blog yazıları
- **Proje Portföyü**: Yapılan projelerin listelenmesi ve detay sayfaları
- **Hakkımda**: Kişisel bilgiler ve deneyimler
- **İletişim Formu**: Ziyaretçilerin iletişime geçebilmesi için form
- **Duyarlı Tasarım**: Tüm cihazlarda düzgün görüntülenen duyarlı arayüz

## Teknolojiler

- ASP.NET Core 8.0
- Entity Framework Core 8.0 (SQLite)
- Bootstrap 5.3
- Markdig (Markdown işleme)
- jQuery (Bootstrap bağımlılığı olarak)

## Kurulum

1. Bu depoyu klonlayın:
   ```bash
   git clone [repo-url]
   cd My-Website
   ```

2. Gerekli paketleri yükleyin:
   ```bash
   dotnet restore
   ```

3. Veritabanını oluşturun ve migration'ları uygulayın:
   ```bash
   dotnet ef database update
   ```

4. Uygulamayı çalıştırın:
   ```bash
   dotnet run
   ```

5. Tarayıcıda `https://localhost:5001` adresini açın.

## Blog Yazısı Ekleme

1. `wwwroot/posts` klasörüne yeni bir `.md` dosyası oluşturun.
2. Dosyanın en üstüne aşağıdaki gibi front matter ekleyin:
   ```yaml
   ---
   title: "Başlık"
   date: 2025-01-01
   tags: ["etiket1", "etiket2"]
   image: "/images/ornek-resim.jpg"
   summary: "Özet metin"
   ---
   ```
3. Markdown formatında içeriğinizi yazın.
4. Uygulamayı yeniden başlattığınızda yazılar otomatik olarak işlenecektir.

## Yapılandırma

Uygulama ayarları `appsettings.json` dosyasından yapılandırılabilir:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=app.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

## Katkıda Bulunma

1. Fork'layın
2. Özellik dalını oluşturun (`git checkout -b feature/AmazingFeature`)
3. Değişikliklerinizi commit'leyin (`git commit -m 'Add some AmazingFeature'`)
4. Dalınıza push'layın (`git push origin feature/AmazingFeature`)
5. Pull Request açın

## Lisans

Bu proje [MIT](LICENSE) lisansı altında lisanslanmıştır.
