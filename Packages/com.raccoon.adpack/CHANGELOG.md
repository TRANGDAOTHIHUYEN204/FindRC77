# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.0.105] - 2026-11-09

### Changed
- Reload sau khi load fail (`RaccoonAdsInterstitial`, `RaccoonAdsRewarded`, `RaccoonAdsBanner`, `JNativeAdsAppOpen`) giãn dần theo số lần fail liên tiếp: base, x2, x4... tối đa 15s; reset khi load thành công. Trước đây retry cố định 1s (open 5s), no-fill kéo dài → load liên tục.

## [0.0.104] - 2026-11-02

### Fixed
- `RaccoonAdsFirst`: bọc các lời gọi `RaccoonAdsManager` bằng `#if ENABLE_RACCOON_ADMOB` để không lỗi compile khi chưa bật define. Khi tắt define, loading vẫn chạy bình thường nhưng không load/show first ad.

## [0.0.103] - 2026-11-02

### Fixed
- `GoogleUMPConsent`: retry `RequestConsent()` khi flow kết thúc mà `CanRequestAds()` vẫn false (vd user mới cài mở app lúc offline => `ConsentInformation.Update` lỗi, cả phiên không có ads). Retry khi app resume hoặc khi mạng có lại (poll 5s), tối thiểu 10s giữa 2 lần, dừng khi `CanRequestAds()` true.

## [0.0.102] - 2026-11-02

### Fixed
- AdMob báo "Consent requirement: Low coverage" (request từ EEA/UK/CH thiếu TC string):
  - `AdsManager`: chỉ init mediation khi `consent.CanRequestAds()` là true, và chỉ init 1 lần.
  - `GoogleUMPConsent`: khi watchdog 8s timeout, flow consent vẫn chạy tiếp; callback `Update`/`ConsentForm.Load` đến muộn vẫn hiện form, xong thì bắn `OnDismissed` lần nữa để init ads.
  - `RaccoonAdsManager`: `ShowAd`, `ShowAdUntilLoaded`, `PreloadAdsWaitShow` không gọi `Load()` khi chưa init (trả `onComplete(false)`); `ShowBanner`/`ShowBannerCollapse` gọi trước khi init sẽ được show sau khi init xong.

## [0.0.101] - 2026-10-28

### Added
- `RaccoonAdsFirst`: component màn loading, init ads và show ad đầu tiên (`adTypeFirst`) khi sẵn sàng, tăng tốc thanh loading sau khi show.
- `RaccoonAdsManager.Initialized` và `IsPreloadAd(AD_TYPE)`.

### Changed
- `RaccoonAdsManager.Initialize` bỏ qua nếu đang init hoặc đã init xong.
- README: link UPM chuyển sang `git@github.com:raccoon-studio-tripglobal/jnativeadsplugin.git#release`.

## [0.0.100] - 2026-10-21

### Added
- TikTok: log payload `ad_revenue` khi bật `debug_mode` để đối chiếu với Events Manager.

## [0.0.99] - 2026-10-20

### Added
- TikTok App Events UA module (Android) qua Pangle/TikTok Combined SDK (`pag-sdk` 8.2.0.8): init với consent (TCF vendor 986), gửi `ImpressionLevelAdRevenue` cho AdMob/JNative, nút "Setup TikTok SDK (One Click)" trong Raccoon/AdKit Setup.
- `GoogleAdsMediation.OnPaidInfo` (`AdPaidInfo`: ad unit id, format, `ResponseInfo`).

### Changed
- Tách logic đọc consent IAB TCF ra `UmpTcfConsent` dùng chung cho Unity Ads và TikTok.

## [0.0.4] - 2026-05-11
- Fix send log renvenue Appsflyer on iOS 

## [0.0.3] - 2026-05-11
- Cancel App Open Ad show after Interstitial and Rewarded Ad.

## [0.0.2] - 2026-04-22

### Changed
- Restructured into Unity Package Manager (UPM) layout: runtime scripts moved into `Runtime/`, `Examples/` renamed to `Samples/`, added `package.json`.