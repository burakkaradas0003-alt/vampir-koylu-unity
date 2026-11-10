# Vampir Köylü — Unity

Agrou tarzı 3D Vampir Köylü oyunu (Unity 6, URP). Tek oyuncu + botlarla oynanır.

## Derleme (Unity Build Automation, Editor açmadan)

1. https://cloud.unity.com → yeni proje "Vampir Köylü".
2. DevOps → Build Automation → kaynak deposu: `burakkaradas0003-alt/zuplay`, dal: `unity-vampir`.
3. Proje alt klasörü: `unity/VampirKoylu`.
4. Hedef ekle: **Android**, Unity sürümü **6000.0 (en güncel)**.
5. Gelişmiş ayarlar → **Pre-Export Method Name**: `BuildSetup.PreExport`
   (URP, malzemeler, sahne ve Android ayarlarını kendisi kurar).
6. Çıktı: APK. İmzalama: Unity'nin varsayılan hata ayıklama anahtarı yeterli.

## Klasörler

- `Assets/Scripts` — oyun (Game), köy/ateş/çimen (World), karakter (Rig), arayüz (Hud), açılış (Boot)
- `Assets/Shaders` — çimen, alev, parıltı gölgelendiricileri
- `Assets/Editor/BuildSetup.cs` — derleme öncesi otomatik kurulum
- `Assets/Resources` — modeller (karakter, kamp ateşi, çimen), dokular, Twemoji görselleri (CC-BY 4.0)
