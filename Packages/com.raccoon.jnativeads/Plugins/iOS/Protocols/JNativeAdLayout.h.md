# JNativeAdLayout.h

## Tổng quan
`JNativeAdLayout` là protocol định nghĩa interface cho tất cả các native ad layouts. Các class như `JBannerLayout`, `JPortraitLayout`, `JLandscapeLayout` đều implement protocol này.

## Import
```objective-c
#import <UIKit/UIKit.h>
#import <GoogleMobileAds/GoogleMobileAds.h>
```

## Protocol Definition
```objective-c
@protocol JNativeAdLayout <NSObject>

- (GADNativeAdView *)getAdView;
- (UIView *)getQuitContainer;
- (void)renderWithNativeAd:(GADNativeAd *)nativeAd;
- (void)destroy;
- (void)show;
- (void)hide;

@end
```

## Required Methods

| Method | Return Type | Description |
|--------|-------------|-------------|
| `getAdView` | `GADNativeAdView *` | Trả về GADNativeAdView để SDK tracking |
| `getQuitContainer` | `UIView *` | Trả về container cho quit button |
| `renderWithNativeAd:` | `void` | Render ad content vào views |
| `destroy` | `void` | Cleanup và giải phóng resources |
| `show` | `void` | Hiển thị ad |
| `hide` | `void` | Ẩn ad |

## Method Details

### getAdView
```objective-c
- (GADNativeAdView *)getAdView;
```
Trả về `GADNativeAdView` - view chính chứa ad. View này phải được setup đúng với các subviews registered:
- `headlineView`
- `bodyView`
- `callToActionView`
- `iconView`
- `mediaView`
- etc.

### getQuitContainer
```objective-c
- (UIView *)getQuitContainer;
```
Trả về container view để `JQuitMethod` có thể thêm close button vào.

### renderWithNativeAd:
```objective-c
- (void)renderWithNativeAd:(GADNativeAd *)nativeAd;
```
Render nội dung từ `GADNativeAd` vào các views:
- Headline → headlineLabel
- Body → bodyLabel
- CTA → callToActionButton
- Icon → iconImageView
- Media → mediaView
- etc.

### destroy
```objective-c
- (void)destroy;
```
Cleanup:
- Remove views từ superview
- Clear media content
- Set references to nil

### show / hide
```objective-c
- (void)show;
- (void)hide;
```
Hiển thị/ẩn ad bằng cách set `containerView.hidden`.

## Implementations

| Class | Template Type | Description |
|-------|---------------|-------------|
| `JBannerLayout` | Small | Compact horizontal layout |
| `JPortraitLayout` | Medium (Portrait) | Vertical layout với media ở trên |
| `JLandscapeLayout` | Medium (Landscape) | Horizontal layout với media bên trái |

## Class Diagram
```
             ┌─────────────────┐
             │ JNativeAdLayout │ (Protocol)
             └────────┬────────┘
                      │
         ┌────────────┼────────────┐
         │            │            │
         ▼            ▼            ▼
┌─────────────┐ ┌───────────────┐ ┌────────────────┐
│JBannerLayout│ │JPortraitLayout│ │JLandscapeLayout│
└─────────────┘ └───────────────┘ └────────────────┘
```

## Xem thêm
- [JBaseNativeLayout.h.md](../Layouts/JBaseNativeLayout.h.md) - Base implementation
- [JBannerLayout.h.md](../Layouts/JBannerLayout.h.md) - Small template
- [JPortraitLayout.h.md](../Layouts/JPortraitLayout.h.md) - Medium portrait
- [JLandscapeLayout.h.md](../Layouts/JLandscapeLayout.h.md) - Medium landscape
