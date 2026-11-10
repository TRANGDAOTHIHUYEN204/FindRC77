# JNative Ads

Native Android & iOS overlay ads integration for Unity, built on Google Mobile Ads SDK.

## Installation

This package is currently distributed in-place under `Assets/JNativeAds/`. Future versions may be published as a Unity Package Manager git package.

## Requirements

- Unity 2021.3 or later
- Add package to your project via the Unity Package Manager: `git@github.com:raccoon-studio-tripglobal/jnativeadsplugin.git#release`
- Google Mobile Ads Unity SDK (imported separately as a `.unitypackage`)
- External Dependency Manager for Unity (EDM4U) — handles Android/iOS native dependencies via `Editor/JNativeAdsDependencies.xml`

## Sample

A sample scene is included under `Samples/Main.unity` along with `Samples/AdsGameExample.cs`.

## How to add Scope Google Mobile Ads Unity SDK via UMP
- Open `Editor/PlayerSettings/Package Manager`
- Add scope: 
  - url https://package.openupm.com
  - scope com.google

## TikTok App Events (UA, Android)

Dùng module TikTok trong Pangle/TikTok Combined SDK (`pag-sdk` >= 7.7) để gửi install/launch + ad revenue cho chương trình TikTok IAA (D7 VBO). Không show ads Pangle.

1. `Raccoon/AdKit Setup` > **Setup TikTok SDK (One Click)**: tạo `Assets/Editor/RaccoonTikTokDependencies.xml`, thêm define `ENABLE_TIKTOK_SDK`, tạo `Assets/RaccoonAds/Resources/TikTokSettings.asset` (cần bật `ENABLE_RACCOON_ADMOB`).
2. Chạy `Assets > External Dependency Manager > Android Resolver > Force Resolve`. Project dùng custom gradle template thì bật **Patch settingsTemplate.gradle** để có repo Pangle.
3. Nhập vào `TikTokSettings` (bên UA lấy trong TikTok Events Manager > App > Settings):
   - `tiktok_app_id_android`: TikTok App ID (nhiều Ad Account thì nối bằng dấu phẩy, không có khoảng trắng).
   - `app_secret_android`: App Secret.
   - App ID = package name (`Application.identifier`), phải khớp App đã tạo trên Events Manager.
4. Test: bật `debug_mode`, build lên máy, xem `InstallApp` / `LaunchAPP` / `ImpressionLevelAdRevenue` ở Events Manager > Test Events. **Tắt `debug_mode` trước khi release.**

Event chỉ được gửi sau khi UMP xong và user có consent (ngoài vùng GDPR luôn gửi); nếu user từ chối, event chỉ nằm local.

## License

Proprietary — Raccoon.