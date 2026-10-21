# Char Haptic

Rung device (haptic) cho Android + iOS, tách ra từ RC67. Không phụ thuộc Olympus, Odin, DOTween hay
service locator nào.

## Cài

*Assets → Import Package → Custom Package…* → chọn `CharHaptic.unitypackage`. Mọi thứ nằm trong
`Assets/Plugins/CharHaptic/`. Project đích cần có uGUI (`com.unity.ugui`, mặc định có sẵn) vì
`HapticToggle` dùng `UnityEngine.UI.Toggle`.

Build target phải là Android hoặc iOS thì mới rung thật; trong Editor/Standalone mọi lời gọi đều
không làm gì.

## Dùng

```csharp
using Char.Haptic;

Haptics.Vibrate(VibrateStyle.Medium);   // Light / Medium / Heavy
Haptics.Light();
Haptics.Heavy();
Haptics.Play(HapticTypes.Success);      // Selection, Success, Warning, Failure (MoreMountains.NiceVibrations)

Haptics.Enabled = false;                // lưu vào PlayerPrefs key "GameVibrate", mặc định bật
```

Màn Settings: thêm component `HapticToggle` vào một `UnityEngine.UI.Toggle`.

Muốn chỉnh độ mạnh/độ dài rung trên Android: sửa các field static `MMVibrationManager.LightDuration`,
`LightAmplitude`, … trong `Runtime/NiceVibrations/MMVibrationManager.cs` (các pattern được dựng lúc
khởi tạo class, nên gán lúc runtime sẽ không ăn — phải sửa giá trị trong file).

## Ghi chú

- Quyền `android.permission.VIBRATE` được Unity tự thêm vì code có tham chiếu `Handheld.Vibrate`,
  không cần sửa AndroidManifest.
- iOS dùng `Runtime/Plugins/iOS/iOSHapticInterface.m` (UIFeedbackGenerator), chỉ bật cho iOS.
- **Xung đột:** nếu project đích đã có Nice Vibrations (asset store hoặc git package
  `com.more.mountains.virbration`) thì sẽ trùng type `MoreMountains.NiceVibrations.*` và trùng
  symbol Objective-C khi build iOS. Gỡ bản cũ trước khi cài package này.

### Thay cho `VibrateService` (Olympus) của RC62/RC67

Nếu project đích dùng Olympus và muốn giữ `Service.Get<VibrateService>()`, viết lại service thành
lớp mỏng gọi `Haptics`:

```csharp
[Serializable]
public class VibrateService : IService<VibrateService>
{
    public bool VibrateStatus { get => Haptics.Enabled; set => Haptics.Enabled = value; }
    public void VibrateLight()  => Haptics.Light();
    public void VibrateMedium() => Haptics.Medium();
    public void VibrateHeavy()  => Haptics.Heavy();
}
```

Key PlayerPrefs giữ nguyên `"GameVibrate"`, nên setting cũ của người chơi vẫn còn.
