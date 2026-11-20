# Add appsflyer
- Add package manager: https://github.com/AppsFlyerSDK/appsflyer-unity-plugin.git#upm
- Add Script Compilation: ENABLE_APPSFLYER
# Configure
- Create object in scene and add component GameAppsFlyer
- Add App ID in inspector: `appIdAndroid` (package name, để trống = Application.identifier), `appIdIOS` (App Store ID dạng số, bắt buộc khi build iOS). `appID` cũ chỉ là fallback.
- Build iOS sẽ fail nếu thiếu define ENABLE_APPSFLYER (khi có plugin AppsFlyer) hoặc App ID iOS không phải dạng số.
- Add App Dev Key in inspector
# Use
- Call GameAppsFlyer.SendAdRevenue()
# Nguồn cài (Organic / Non-organic), chỉ Android
- `GameAppsFlyer.IsOrganic`: `true` Organic, `false` Non-organic, `null` chưa biết. Lấy từ `af_status` trong conversion data, lưu vào PlayerPrefs nên các lần mở sau có ngay.
- `GameAppsFlyer.OnOrganicResolved += organic => ...`: đăng ký lúc đã có kết quả thì được gọi ngay, gọi lại nếu kết quả đổi.
- Ví dụ nối với package noti: `GameAppsFlyer.OnOrganicResolved += RaccoonNoti.SetOrganic;`
- **iOS:** không đọc nguồn cài (code bị loại khi build iOS), `IsOrganic` luôn `null` và event không bao giờ gọi. Mục đích là game không chạy khác nhau giữa người review của Apple (luôn organic) và người dùng thật, tránh bị reject theo Guideline 2.3.1. Đừng tự đọc `af_status` trên iOS để bật/tắt tính năng.
- Không có define `ENABLE_APPSFLYER` thì API vẫn có (để code game compile được) nhưng không bao giờ có kết quả.
