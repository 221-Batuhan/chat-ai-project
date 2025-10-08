Chat AI Project — Monorepo README (intern notlarımla)

Bu repo 3 ana parçadan oluşuyor: web (frontend), API (backend) ve basit bir AI servis (ai-service). Aşağıdaki adımlarla projeyi kendi makinemde kurup çalıştırdım. Linkleri ve yapı açıklamalarını olabildiğince kısa ve net tuttum.

Canlı Linkler (Demo)
- Web Chat (Vercel): https://chat-ai-project-five.vercel.app/  ← buraya Vercel linkini koyuyorum
- Mobil (React Native): iOS Simulator’da çalıştırdım, Android için APK: Android için test edemedim o yüzden malesef koyamıyorum apk linkini.
- AI Service (Hugging Face Space): https://huggingface.co/spaces/batuhan-221/chat-sentiment-ai
- Backend API (Render): https://chat-ai-project.onrender.com

Kullanılan AI Araçları
- ChatGPT (OpenAI): metin yardımcısı olarak; prompt ve dokümantasyon yazımında destek aldım
- Gradio: AI servisi için hızlı UI ve local demo
- (Opsiyonel) Hugging Face Transformers/Pipeline: sentiment modeli kullanımı (app.py içinde)
- Not: Üretim için hafif ve hızlı bir sentiment servisi planlandı; gerekirse model değiştirilebiliyor

Monorepo Yapısı
- frontend/web-chat: React web sohbet arayüzü
  - src/App.js: uygulama kabuğu
  - src/components/: sohbet bileşenleri (UI)
  - public/index.html: giriş noktası
  - package.json: komutlar ve bağımlılıklar
- backend/ChatApi: .NET 9 Web API (SQLite + EF Core)
  - Program.cs: app başlangıç ve pipeline
  - Controllers/MessageController.cs & UsersController.cs: REST uçları
  - Data/AppDbContext.cs: EF Core context ve DbSet’ler
  - Models/Message.cs, User.cs, SentimentResult.cs: veri modelleri
  - Migrations/: EF migration’lar
  - appsettings.json(.Development): bağlantılar/ayarlar
- mobile: React Native mobil istemci
  - App.tsx → src/ChatScreen.js: sohbet ekranı
  - ios/: Xcode proje dosyaları, Podfile (CocoaPods)
  - android/: Gradle ayarları
  - src/config.js: API_URL (backend’e istek için)
- ai-service: Gradio tabanlı basit sentiment servisi
  - app.py: Gradio arayüzü ve model çağrısı (lokal demo)
  - README.md: çok kısa kurulum & çalıştırma

Kurulum — Hızlı Başlangıç

Önkoşullar
- Node 18+, npm
- .NET SDK 9
- Xcode + CocoaPods (iOS) / Android Studio (Android) [mobil için]
- Python 3.10+ (AI servis için)

Backend (API)
```bash
cd /Users/batuhanacan/Desktop/chat-ai-project/backend/ChatApi
dotnet restore
dotnet ef database update  # İlk kurulumda (migrasyonlar için)
dotnet run                # http://localhost:5135
```

Frontend (Web Chat)
```bash
cd /Users/batuhanacan/Desktop/chat-ai-project/frontend/web-chat
npm i
npm start                 # http://localhost:3000
```

Mobil (React Native)
```bash
# 1) Bağımlılıklar
cd /Users/batuhanacan/Desktop/chat-ai-project/mobile
npm i

# 2) iOS (ilk kez): CocoaPods
cd ios
pod install               # sorun olursa: pod install --clean-install --no-repo-update --verbose
cd ..

# 3) Metro bundler
npm run start

# 4) iOS Simulator
npm run ios               # veya Xcode’dan .xcworkspace ile ▶️

# Android (opsiyonel)
npm run android
```

AI Service (Gradio)
```bash
cd /Users/batuhanacan/Desktop/chat-ai-project/ai-service
python3 -m venv .venv && source .venv/bin/activate
pip install --upgrade pip gradio
python app.py            # http://127.0.0.1:7860
```

Konfig (API URL)
- Mobil: `mobile/src/config.js` içinde `API_URL` var. Prod default: `https://chat-ai-project.onrender.com`
- Lokal test: `API_URL = "http://localhost:5135"` yapıp backend’i lokal çalıştırdım

Kod Hakimiyeti (Benim elle yazdıklarım vs. AI yardımı)
- Elle yazdığım kritik kısımlar (örnekler):
  - Backend controller’larda basit CRUD akışı ve EF Core sorguları
  - Mobil `ChatScreen.js` içinde mesaj listeleme ve gönderme akışı
  - Web’de temel state yönetimi ve fetch çağrısı
- AI yardımıyla hızlandırdığım kısımlar:
  - UI düzen önerileri ve basit CSS iyileştirmeleri
  - AI servisinde Gradio arayüz iskeleti

Küçük Kod Örneği (elle yazdığım tarzda)
```csharp
// backend/ChatApi/Controllers/MessageController.cs içindeki tarzda basit bir okuma
// (Tam kod değil, örnek stil)
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class MessageController : ControllerBase
{
    private readonly AppDbContext db;
    public MessageController(AppDbContext db) { this.db = db; }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var messages = await db.Messages
            .OrderBy(m => m.Timestamp)
            .ToListAsync();
        return Ok(messages);
    }
}
```

Troubleshooting (Kısa)
- CocoaPods glog’da takılırsa: `brew install autoconf automake libtool pkg-config gperf cmake python` ve sonra `pod cache clean --all` + `pod install`
- Xcode “Pods-*.xcconfig bulunamadı”: `cd ios && pod install` ve `.xcworkspace` ile aç
- Git’e bin/obj/node_modules/Pods girmişse: `.gitignore` ekleyip `git rm -r --cached` ile temizlemek gerekir

Lisans & Notlar
- Projenin lisansı üst dizinde belirtilen lisansa tabidir.
- Canlı linkler ve build/akış notları zamanla güncellenebilir.


