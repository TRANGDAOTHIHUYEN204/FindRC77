# JNativeOverlay.h

## Tổng quan
`JNativeOverlay` là class chính quản lý Native Overlay Ads. Class này xử lý việc load, hiển thị, ẩn và destroy native ads, đồng thời quản lý layout và quit method.

## Import
```objective-c
#import <UIKit/UIKit.h>
#import <GoogleMobileAds/GoogleMobileAds.h>
#import "JNativeOverlayCallback.h"
#import "JNativeAdLayout.h"
#import "JQuitMethod.h"
```

## Protocol Implementation
```objective-c
@interface JNativeOverlay : NSObject <GADNativeAdLoaderDelegate, GADNativeAdDelegate>
```

## Properties (Read-only)

| Property | Type | Description |
|----------|------|-------------|
| `x` | `CGFloat` | Vị trí X của ad |
| `y` | `CGFloat` | Vị trí Y của ad |
| `width` | `CGFloat` | Chiều rộng của ad |
| `height` | `CGFloat` | Chiều cao của ad |
| `paddingStart` | `CGFloat` | Padding bên trái |
| `paddingEnd` | `CGFloat` | Padding bên phải |
| `paddingTop` | `CGFloat` | Padding trên |
| `paddingBottom` | `CGFloat` | Padding dưới |

## Initialization

```objective-c
- (instancetype)initWithCallback:(id<JNativeOverlayCallback>)callback
                          unitID:(NSString *)unitID
                         refresh:(NSTimeInterval)refresh
                               x:(CGFloat)x
                               y:(CGFloat)y
                           width:(CGFloat)width
                          height:(CGFloat)height
                        template:(NSString *)templateName
                     orientation:(NSString *)orientation
                        quitType:(NSString *)quitType
                    paddingStart:(CGFloat)paddingStart
                      paddingEnd:(CGFloat)paddingEnd
                      paddingTop:(CGFloat)paddingTop
                   paddingBottom:(CGFloat)paddingBottom
                           muted:(BOOL)muted;
```

### Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `callback` | `id<JNativeOverlayCallback>` | Callback để nhận events |
| `unitID` | `NSString *` | AdMob Unit ID |
| `refresh` | `NSTimeInterval` | Thời gian refresh (giây), 0 = không refresh |
| `x`, `y` | `CGFloat` | Vị trí ad |
| `width`, `height` | `CGFloat` | Kích thước ad |
| `templateName` | `NSString *` | Template: `"small"`, `"medium"`, `"banner"` |
| `orientation` | `NSString *` | Orientation: `"portrait"`, `"landscape"` |
| `quitType` | `NSString *` | Loại quit: `"inter"`, `"native"`, `"none"` |
| `padding*` | `CGFloat` | Các giá trị padding |
| `muted` | `BOOL` | Video có muted không |

## Public Methods

```objective-c
/// Load native ad
- (void)load;

/// Hiển thị native ad (phải load trước)
- (void)show;

/// Ẩn native ad
- (void)hide;

/// Destroy native ad và giải phóng resources
- (void)destroy;

/// Xử lý app pause/resume
- (void)onApplicationPause:(BOOL)pause;
```

## Template Selection

| templateName | orientation | Layout Used |
|--------------|-------------|-------------|
| `"small"` | any | `JBannerLayout` |
| `"banner"` | any | `JBannerLayout` |
| `"medium"` | `"portrait"` | `JPortraitLayout` |
| `"medium"` | `"landscape"` | `JLandscapeLayout` |
| other | `"landscape"` | `JLandscapeLayout` |
| other | other | `JPortraitLayout` (default) |

## Quit Types

| quitType | Class Used | Description |
|----------|------------|-------------|
| `"inter"` | `JInterClose` | Interstitial style close button |
| `"native"` | `JNativeClose` | Native style close button |
| `"none"` | `JNoneClose` | Không có nút close |

## Xem thêm
- [JNativeOverlay.m.md](JNativeOverlay.m.md) - Implementation
- [JNativeOverlayCallback.h.md](JNativeOverlayCallback.h.md) - Callback protocol
- [Layouts/README.md](Layouts/README.md) - Layout templates
