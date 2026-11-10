# JNativeOverlayCallback.h

## Tổng quan
`JNativeOverlayCallback` là protocol định nghĩa các callback events cho Native Overlay Ads. Protocol này được implement bởi Unity bridge để nhận các events từ native code.

## Import
```objective-c
#import <Foundation/Foundation.h>
#import <GoogleMobileAds/GoogleMobileAds.h>
```

## Protocol Definition
```objective-c
@protocol JNativeOverlayCallback <NSObject>

- (void)onLoadSucceed;
- (void)onLoadFailedWithError:(NSError *)error;
- (void)onClosed;
- (void)onClicked;
- (void)onImpression;
- (void)onAdPaidWithValue:(GADAdValue *)value;

@end
```

## Callback Methods

| Method | Description | When Called |
|--------|-------------|-------------|
| `onLoadSucceed` | Ad loaded thành công | Sau khi `GADAdLoader` nhận được ad |
| `onLoadFailedWithError:` | Load ad thất bại | Khi có lỗi load ad |
| `onClosed` | Ad bị đóng | Khi user đóng ad hoặc gọi `hide` |
| `onClicked` | User click vào ad | Khi user tap vào ad |
| `onImpression` | Ad được hiển thị | Khi gọi `show` thành công |
| `onAdPaidWithValue:` | Ad được paid | Khi nhận được revenue event |

## Error Handling

```objective-c
- (void)onLoadFailedWithError:(NSError *)error {
    // error.localizedDescription - Mô tả lỗi
    // error.code - Error code
    // error.domain - Error domain (GADErrorDomain)
}
```

### Common Error Codes (GADErrorDomain)
| Code | Description |
|------|-------------|
| 0 | Invalid request |
| 1 | No ad to show |
| 2 | Network error |
| 3 | Server error |

## Paid Event

```objective-c
- (void)onAdPaidWithValue:(GADAdValue *)value {
    // value.value - Revenue value (NSDecimalNumber)
    // value.currencyCode - Currency (e.g., "USD")
    // value.precision - Precision level
}
```

### Precision Levels
| Value | Description |
|-------|-------------|
| `GADAdValuePrecisionUnknown` | Unknown precision |
| `GADAdValuePrecisionEstimated` | Estimated value |
| `GADAdValuePrecisionPublisherProvided` | Publisher provided |
| `GADAdValuePrecisionPrecise` | Precise value |

## Usage Example

```objective-c
@interface MyCallbackHandler : NSObject <JNativeOverlayCallback>
@end

@implementation MyCallbackHandler

- (void)onLoadSucceed {
    NSLog(@"Ad loaded successfully");
}

- (void)onLoadFailedWithError:(NSError *)error {
    NSLog(@"Ad failed to load: %@", error.localizedDescription);
}

- (void)onClosed {
    NSLog(@"Ad closed");
}

- (void)onClicked {
    NSLog(@"Ad clicked");
}

- (void)onImpression {
    NSLog(@"Ad impression recorded");
}

- (void)onAdPaidWithValue:(GADAdValue *)value {
    NSLog(@"Ad paid: %@ %@", value.value, value.currencyCode);
}

@end
```

## Xem thêm
- [JNativeOverlay.h.md](JNativeOverlay.h.md) - Main class
- [Bridge/JNativeAdsBridge.mm.md](Bridge/JNativeAdsBridge.mm.md) - Unity bridge implementation
