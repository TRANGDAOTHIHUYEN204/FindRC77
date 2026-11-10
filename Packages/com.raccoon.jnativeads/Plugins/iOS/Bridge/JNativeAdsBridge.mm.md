# JNativeAdsBridge.mm

## Tổng quan
`JNativeAdsBridge.mm` là bridge layer giữa Unity (C#) và native iOS code. File này exports các C functions cho Unity DllImport và quản lý các instances của native overlays.

## Import
```objective-cpp
#import <Foundation/Foundation.h>
#import <objc/runtime.h>
#import "JNativeOverlay.h"
#import "JNativeOverlayCallback.h"
```

## JUnityNativeOverlayCallback

### Class Definition
```objective-cpp
@interface JUnityNativeOverlayCallback : NSObject <JNativeOverlayCallback>
@property (nonatomic, copy) NSString *instanceId;
@property (nonatomic, copy) NSString *gameObjectName;
@end
```

### Callback Implementation
Chuyển đổi native events thành UnitySendMessage calls:

```objective-cpp
- (void)sendMessageToUnity:(NSString *)method message:(NSString *)message {
    // Format: "instanceId|data"
    NSString *fullMessage = [NSString stringWithFormat:@"%@|%@", 
                             self.instanceId, 
                             message ?: @""];
    
    UnitySendMessage(gameObject, methodName, msg);
}
```

### Callback Methods

| Method | Unity Message | Data |
|--------|---------------|------|
| `onLoadSucceed` | `OnLoadSucceed` | `instanceId|` |
| `onLoadFailedWithError:` | `OnLoadFailed` | `instanceId|errorMessage` |
| `onClosed` | `OnClosed` | `instanceId|` |
| `onClicked` | `OnClicked` | `instanceId|` |
| `onImpression` | `OnImpression` | `instanceId|` |
| `onAdPaidWithValue:` | `OnAdPaid` | `instanceId|{json}` |

### Ad Paid JSON Format
```json
{
    "currencyCode": "USD",
    "valueMicros": 1234567,
    "precision": 2
}
```

## Instance Management

```objective-cpp
static NSMutableDictionary<NSString *, JNativeOverlay *> *overlayInstances;
static NSMutableDictionary<NSString *, JUnityNativeOverlayCallback *> *callbackInstances;
```

## C Interface (extern "C")

### JNativeAds_Initialize
```cpp
void JNativeAds_Initialize();
```
Khởi tạo dictionaries nếu chưa có.

### JNativeAds_Create
```cpp
const char* JNativeAds_Create(
    const char* instanceId,
    const char* gameObjectName,
    const char* unitID,
    long refresh,
    float x, float y, float width, float height,
    const char* templateName,      // "small", "medium", "banner"
    const char* orientation,        // "portrait", "landscape"
    const char* quitType,          // "inter", "native", "none"
    float paddingStart, float paddingEnd,
    float paddingTop, float paddingBottom,
    bool muted
);
```
Tạo JNativeOverlay và callback, lưu vào dictionaries.

### JNativeAds_Load
```cpp
void JNativeAds_Load(const char* instanceId);
```
Gọi `[overlay load]` để load ad.

### JNativeAds_Show
```cpp
void JNativeAds_Show(const char* instanceId);
```
Gọi `[overlay show]` để hiển thị ad.

### JNativeAds_Hide
```cpp
void JNativeAds_Hide(const char* instanceId);
```
Gọi `[overlay hide]` để ẩn ad.

### JNativeAds_Destroy
```cpp
void JNativeAds_Destroy(const char* instanceId);
```
Gọi `[overlay destroy]` và remove từ dictionaries.

### JNativeAds_OnApplicationPause
```cpp
void JNativeAds_OnApplicationPause(const char* instanceId, bool pause);
```
Xử lý app pause/resume để quản lý refresh timer.

## Template Selection

| templateName | orientation | Result |
|--------------|-------------|--------|
| `"small"` | any | JBannerLayout (Small Template) |
| `"banner"` | any | JBannerLayout (Small Template) |
| `"medium"` | `"portrait"` | JPortraitLayout (Medium Portrait) |
| `"medium"` | `"landscape"` | JLandscapeLayout (Medium Landscape) |

## Unity Usage (C#)

```csharp
[DllImport("__Internal")]
private static extern string JNativeAds_Create(
    string instanceId,
    string gameObjectName,
    string unitID,
    long refresh,
    float x, float y, float width, float height,
    string templateName,
    string orientation,
    string quitType,
    float paddingStart, float paddingEnd,
    float paddingTop, float paddingBottom,
    bool muted
);

[DllImport("__Internal")]
private static extern void JNativeAds_Load(string instanceId);

[DllImport("__Internal")]
private static extern void JNativeAds_Show(string instanceId);

[DllImport("__Internal")]
private static extern void JNativeAds_Hide(string instanceId);

[DllImport("__Internal")]
private static extern void JNativeAds_Destroy(string instanceId);
```

## Xem thêm
- [JNativeOverlay.h.md](../JNativeOverlay.h.md) - Core overlay class
- [JNativeOverlayCallback.h.md](../JNativeOverlayCallback.h.md) - Callback protocol
