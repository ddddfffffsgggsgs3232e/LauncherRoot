# LauncherRoot

LauncherRoot — Minecraft Fabric Launcher (Açık Kaynak)
Minecraft 1.21.4 için Fabric tabanlı, tek tıkla çalışan bir launcher. Modrinth üzerinden performans modları otomatik kurulur, tema seçimi ile farklı mod paketleri desteklenir.
Özellikler
- Otomatik kurulum — Fabric loader, Minecraft client, asset'ler ve tüm kütüphaneleri kendisi indirir
- Modrinth entegrasyonu — Tek tıkla mod yükleme (Sodium, Lithium, FerriteCore, Krypton, vs.)
- Tema sistemi — Farklı mod paketleri arasından seçim yapın
- Mod yönetimi — Mod arama, yükleme, etkinleştirme/devre dışı bırakma
- RAM ayarı — Minecraft'a ne kadar RAM verileceğini seçin
- Açık/Koyu tema + Türkçe/İngilizce dil desteği
Görseller
Main Menu
<img width="1349" height="715" alt="resim" src="https://github.com/user-attachments/assets/64867a04-5652-467d-8b4e-fa7c7d032f5c" />

# Kurulum
Hiçbir şey kurmanıza gerek yok. Çalıştırılabilir tek dosya olarak derlenmiştir:
# Linux (x64)
chmod +x LauncherRoot
./LauncherRoot
# Windows (x64)
LauncherRoot.exe dosyasına çift tıklayın.
# macOS (Intel / Apple Silicon)
chmod +x LauncherRoot
./LauncherRoot
Not: macOS'ta "Doğrulanmamış geliştirici" uyarısı alırsanız: Ayarlar > Gizlilik ve Güvenlik > Yine de Aç seçeneğini kullanın.
İlk Kullanım
1. Uygulama açılınca kullanıcı adınızı girin
2. Bir tema seçin (mod paketi otomatik indirilir)
3. "Oyna" butonuna basın
4. Launcher, Fabric + Minecraft + asset'leri indirir ve oyunu başlatır
Build (Geliştiriciler İçin)
git clone https://github.com/ddddfffffsgggsgs3232e//LauncherRoot
cd LauncherRoot
dotnet run

Derleme
# Linux
dotnet publish -c Release --self-contained true -r linux-x64 -p:PublishSingleFile=true -o publish-linux

# Windows
dotnet publish -c Release --self-contained true -r win-x64 -p:PublishSingleFile=true -o publish-win

# macOS Intel
dotnet publish -c Release --self-contained true -r osx-x64 -p:PublishSingleFile=true -o publish-mac

# macOS Apple Silicon
dotnet publish -c Release --self-contained true -r osx-arm64 -p:PublishSingleFile=true -o publish-mac-arm
Gereksinimler
- JDK 21 (Minecraft için). Linux: sudo apt install openjdk-21-jdk / Windows: Oracle JDK 21 (https://www.oracle.com/java/technologies/downloads/)
- Yoksa .NET Runtime gerekmez — self-contained derlenmiştir.
# Lisans
MIT — dilediğiniz gibi kullanın, değiştirin, dağıtın.
