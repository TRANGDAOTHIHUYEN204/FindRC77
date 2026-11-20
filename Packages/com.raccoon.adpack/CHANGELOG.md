# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.0.111] - 2026-11-20

### Fixed
- Lỗi compile `CS0246: NativeAdSourceInfo could not be found` ở `JNativeAdsUnit.cs`: `versionDefines` bật `RACCOON_JNATIVE_ADSOURCE` theo version (>= 0.0.109) nhưng `com.raccoon.jnativeads` branch `alpha_test` (0.0.122) chưa có `OnAdPaidInfo`/`NativeAdSourceInfo`. Đã bỏ `versionDefines`.

### Changed
- JNative ad source giờ gate bằng define `JNATIVE_HAS_ADSOURCE` do chính plugin khai báo (`com.raccoon.jnativeads` alpha >= 0.0.124, beta >= 0.0.111, tự thêm vào Scripting Define Symbols). Bỏ `RACCOON_JNATIVE_ADSOURCE`, không cần bật tay. Plugin cũ không có define → dùng `OnAdPaid`, không có ad source.

## [0.0.110] - 2026-11-19

### Added
- `GameAppsFlyer.IsOrganic` và event `OnOrganicResolved`: nguồn cài Organic / Non-organic đọc từ `af_status` của conversion data, lưu PlayerPrefs, đăng ký muộn vẫn nhận được. Chỉ Android; iOS không đọc nguồn cài (luôn `null`, event không gọi) để tránh bị reject vì game chạy khác nhau giữa reviewer và người dùng thật.
- Sửa cảnh báo Play Console "Required hardware feature declarations are restricting your app's availability" (`android.hardware.wifi`, `android.hardware.screen.portrait`...): mỗi lần build Android, `OptionalHardwareFeatures` thêm `<uses-feature android:required="false" tools:replace="android:required">` vào manifest của app (launcher) cho wifi, telephony, location (+network, gps), bluetooth, screen.portrait, screen.landscape. Menu `Raccoon > Fix Android Manifest` ghi các khai báo này vào `Assets/Plugins/Android/AndroidManifest.xml` / `LauncherManifest.xml` nếu có.

## [0.0.109] - 2026-11-10

### Fixed
- `TikTokInitializer`: guard khi build JSON ad revenue: bỏ event khi `AdValue` null, field string null coi như rỗng, exception chỉ log warning (không lan ra các handler `OnPaidInfo` khác).
- `AdSource`: field chuyển thành property `?? string.Empty`, kể cả `default(AdSource)` cũng không trả null.

## [0.0.108] - 2026-11-10

### Fixed
- Ad revenue từ JNative (native, Android/iOS) giờ có ad source/network: trước đây JNative gọi `RaisePaid` không có `ResponseInfo` nên TikTok `ad_source_name` (IAA signal `ad_revenue_network`) luôn rỗng → coverage 0%. Cần `com.raccoon.jnativeads` >= 0.0.109 (`NativeOverlay.OnAdPaidInfo`); `RCKit.Ads.JNative.asmdef` tự bật define `RACCOON_JNATIVE_ADSOURCE` qua `versionDefines`. Với JNative cũ hơn kit vẫn compile, dùng `OnAdPaid` như trước (không có ad source).

### Changed
- `AdPaidInfo` thêm `Source` (`AdSource`: name, id, instance name/id, mediation group, A/B test name/variant). Ad AdMob thường lấy từ `ResponseInfo`, JNative lấy từ plugin. Thêm overload `GoogleAdsMediation.RaisePaid(adValue, adUnitId, adFormat, AdSource)`.
- `TikTokInitializer` đọc ad source từ `AdPaidInfo.Source`.
- `GameAppsFlyer`: tách App ID theo platform: `appIdAndroid` (trống = `Application.identifier`) và `appIdIOS` (App Store ID dạng số). Field `appID` cũ giữ lại làm fallback khi ô platform để trống, scene cũ không cần sửa để build Android.
- Raccoon Ads Setup > Setup AppsFlyer: ghi vào `appIdAndroid`/`appIdIOS` theo build target hiện tại; iOS bắt buộc ID dạng số.

### Added
- `AppsFlyerBuildValidator` (Editor): build iOS fail nếu có plugin AppsFlyer nhưng thiếu define `ENABLE_APPSFLYER`, hoặc `GameAppsFlyer` trong scene build có App ID iOS không phải dạng số (ví dụ còn để package name Android). Cảnh báo nếu bật define mà không có `GameAppsFlyer` nào trong scene build.
- Runtime iOS log error nếu App ID không hợp lệ.

## [0.0.107] - 2026-11-10

### Fixed
- `GameAppsFlyer` (iOS): gọi `AppsFlyer.waitForATTUserAuthorizationWithTimeoutInterval` trước `startSDK()` để SDK giữ install/`first_open` lại tới khi user trả lời ATT (hoặc hết timeout). Trước đây install gửi đi trước popup ATT (ATT chỉ hiện sau UMP) nên phần lớn install thiếu IDFA.

### Added
- `GameAppsFlyer.attWaitTimeoutSeconds` (mặc định 60s, 0 = không chờ). Timeout tính từ lúc `startSDK()`, nên thời gian user ở form UMP cũng bị trừ vào đây.

## [0.0.106] - 2026-11-10

### Added
- Hỗ trợ multi ad với 3 id:
  - `AdUnitConfig`: thêm `is_multi_ad_3`, `ad_id_android_3`/`ad_id_ios_3` (chỉ có tác dụng khi `is_multi_ad` bật), property `ad_id_3` và `GetMultiAdExtraIds()`. Asset cũ không có field mới vẫn giữ hành vi 2 id.
  - `JNativeAdsInterstitial.SetMultiAd(params string[])` nhận nhiều ad id phụ (bỏ qua id rỗng); `Load()` ghép id dạng `id1_id2` hoặc `id1_id2_id3`.
  - `RaccoonAdsManager.Init` truyền toàn bộ id phụ từ `GetMultiAdExtraIds()` vào `SetMultiAd`.
  - `BuildTestAdUnitConfigs` copy `is_multi_ad_3` và dùng test ad id cho id thứ 3 khi bật.
  - `AdsScriptableObjEditor`: thêm toggle "Is Multi Ad 3" và ô Ad Id 3 (Android/iOS), báo lỗi khi để trống.

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