# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.0.124] - 2026-11-20

### Changed
- Alpha đồng bộ toàn bộ plugin từ `dev_beta_test` (0.0.110): thêm multi-ad iOS, OnAdPaidInfo/NativeAdSourceInfo, fix OOM. Các thay đổi riêng của alpha trước đó (0.0.111–0.0.123) bị thay thế.
- AAR build từ branch Android `fix_policy` (gồm toàn bộ `dev_beta_test` Android + chỉnh layout non-FAN: quit tap 15dp, media dưới quit bar, `media_view` hiện mặc định).

### Added
- Scripting define `JNATIVE_HAS_ADSOURCE`, plugin tự thêm vào Player Settings (Android/iOS/Standalone) qua `JNativeFeatureDefines` (Editor). Package khác nên dùng `#if JNATIVE_HAS_ADSOURCE` thay vì đoán theo version.

## [0.0.110] - 2026-11-11

### Fixed
- Android (multi-ad `inter2`): click vào ad cuối (hoặc khi ad sau chưa sẵn sàng) mà app không rời sang Store/web — hoặc `onAdClicked` đến sau khi app đã resume — khiến thanh loading dừng và mất nút X, kẹt ad. Giờ sau 0,25s thanh loading fill đầy + hiện lại X, ~1s sau tự đóng nếu app không bị pause.
- Android: `onAdClicked` đến muộn sau khi overlay đã hide/destroy chỉ báo `OnClicked`, không kích hoạt logic chuyển/đóng ad.

## [0.0.109] - 2026-11-10

### Added
- Paid event kèm ad source của đúng ad vừa trả tiền (kể cả multi-ad): `NativeOverlay.OnAdPaidInfo` (`AdValue`, `NativeAdSourceInfo`) với `ad_source_name`, `ad_source_id`, `ad_source_instance_name`, `ad_source_instance_id`, `adapter_class_name`, `response_id`, `mediation_group_name`, `mediation_ab_test_name`, `mediation_ab_test_variant`. `OnAdPaid` cũ vẫn giữ nguyên.
- Android: callback Java đổi `onAdPaid(AdValue)` → `onAdPaidWithInfo(AdValue, String json)` (AAR mới). iOS: `onAdPaidWithValue:adSourceInfo:`, ad source được ghép vào JSON `OnAdPaid`.

### Changed
- `INativeOverlay.OnAdPaid` thay bằng `OnAdPaidInfo`. Code ngoài subscribe trực tiếp client (`GetClient()`) cần đổi sang event mới.

## [0.0.108] - 2026-11-09

### Changed
- Android: bỏ `ad_click_overlay` khỏi layout `native_portrait` và `native_landscape_2` — vùng trống của 2 template này không còn chuyển tap thành click CTA.

## [0.0.107] - 2026-11-09

### Fixed
- Android: OOM do rò rỉ mỗi overlay đã load. `AndroidNativeOverlay.Destroy()` giờ `Dispose()` global ref tới Java `UnityNativeOverlay` (vòng tham chiếu chéo với `AndroidJavaProxy` khiến cả 2 phía không bao giờ được GC), và Java side bỏ tham chiếu `callback` khi destroy.
- Android: `destroy()` gọi `NativeAdView.destroy()`, xoá icon drawable, huỷ CountDownTimer/ValueAnimator của các quit method (InterClose/InterClose2/InterClose3/NativeClose); `destroy()` idempotent.
- Android: khôi phục consumer ProGuard keep rule cho `UnityNativeOverlay`/`UnityNativeOverlayCallback`.

## [0.0.85] - 2026-08-15

> Native-side (`.aar`/iOS bridge) changes between 0.0.17 and 0.0.85 were shipped as many small incremental patch bumps without individual changelog entries. This entry consolidates the notable changes from that range.

### Added
- Close (quit) button scale controls: `SetScaleButtonQuit` and `SetScaleButtonQuitCover`, plus `SetTimeClose` for the tappable delay.
- New templates: `TEMPLATE_FULLSCREEN2`, `TEMPLATE_FULLSCREEN_MEDIA_2`, `TEMPLATE_LANDSCAPE_FAN`, `TEMPLATE_LANDSCAPE_BANNER`, and additional native/KB quit layouts.
- `SetReloadOnClick` and `refresh = -1` to disable auto-refresh.
- `SetEnforceMinViewTimeBeforeHide` — minimum view time before an ad can be hidden (show-rate optimization).
- `SetBannerTheme` (light/dark) and `SetRefreshTime` for Android banners.
- Automatic mute/pause of the Unity app while a full-screen native ad is shown.

### Changed
- Reworked portrait layout and banner collapse behavior (Android/iOS).
- Reworked the `OnClosed` callback plumbing on Android/iOS/Unity.
- Added a click delay and clamped the ad view to stay inside its bounds to reduce accidental/invalid clicks.
- Updated banner UI (legacy layout, white background) and Ad Choices icon position (moved to top-right on Android).

### Fixed
- Various layout, lifecycle (pause/resume), and click-tracking bugs across Android and iOS reported over this range.

### Policy
- Hid/re-enabled the in-ad "open store" button to stay compliant with Google Play policy.

## [0.0.16] - 2026-05-11

### Changed
- Update method SetPauseOnShow.
- Add method OnResetEvent to reset the all events.

## [0.0.9] - 2026-05-04

### Changed
- Move icon Ad Choices to Top right corner for Android.
- Add method SetPauseOnShow to pause the game when the ad is shown.

## [0.0.5] - 2026-04-08

### Changed
- Restructured into Unity Package Manager (UPM) layout: runtime scripts moved into `Runtime/`, `Examples/` renamed to `Samples/`, added `package.json`.
- `RectTransformDebugger.cs` moved into `Samples/` (used only by the sample scene).

### Removed
- `version.txt` (version is now tracked in `package.json`).
- Markdown doc sidecars: `IOSNativeOverlay.cs.md`, `LoadAdError.cs.md`.