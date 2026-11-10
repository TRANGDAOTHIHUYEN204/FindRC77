# JQuitMethod.h

## Tổng quan
`JQuitMethod` là protocol định nghĩa interface cho các phương thức đóng (quit/close) native ad. Các implementation khác nhau cung cấp các cách đóng ad khác nhau.

## Import
```objective-c
#import <Foundation/Foundation.h>
```

## Protocol Definition
```objective-c
@protocol JQuitMethod <NSObject>

- (void)onShow;
- (void)destroy;

@end
```

## Required Methods

| Method | Description |
|--------|-------------|
| `onShow` | Được gọi khi ad được hiển thị - setup close button nếu cần |
| `destroy` | Cleanup và giải phóng resources |

## Method Details

### onShow
```objective-c
- (void)onShow;
```
Được gọi khi ad được hiển thị. Các implementation có thể:
- Hiển thị close button
- Bắt đầu countdown timer
- Setup gesture recognizers

### destroy
```objective-c
- (void)destroy;
```
Cleanup:
- Remove close button từ superview
- Invalidate timers
- Remove gesture recognizers
- Set references to nil

## Implementations

| Class | Type | Description |
|-------|------|-------------|
| `JInterClose` | `"inter"` | Interstitial style - close button hiển thị sau countdown |
| `JNativeClose` | `"native"` | Native style - close button luôn hiển thị |
| `JNoneClose` | `"none"` | Không có close button - ad phải được đóng programmatically |

## Usage in JNativeOverlay

```objective-c
- (void)createLayout {
    // ... create layout ...
    
    UIView *quitContainer = [self.layout getQuitContainer];
    
    if ([self.quitType isEqualToString:@"inter"]) {
        self.quitMethod = [[JInterClose alloc] initWithNativeOverlay:self 
                                                        quitContainer:quitContainer];
    } else if ([self.quitType isEqualToString:@"native"]) {
        self.quitMethod = [[JNativeClose alloc] initWithNativeOverlay:self 
                                                         quitContainer:quitContainer];
    } else {
        self.quitMethod = [[JNoneClose alloc] init];
    }
}
```

## Class Diagram
```
           ┌────────────┐
           │ JQuitMethod│ (Protocol)
           └─────┬──────┘
                 │
     ┌───────────┼───────────┐
     │           │           │
     ▼           ▼           ▼
┌──────────┐ ┌───────────┐ ┌──────────┐
│JInterClose│ │JNativeClose│ │JNoneClose│
└──────────┘ └───────────┘ └──────────┘
```

## Xem thêm
- [JInterClose.h.md](../QuitMethods/JInterClose.h.md) - Interstitial style
- [JNativeClose.h.md](../QuitMethods/JNativeClose.h.md) - Native style
- [JNoneClose.h.md](../QuitMethods/JNoneClose.h.md) - No close button
- [JNativeOverlay.m.md](../JNativeOverlay.m.md) - Usage example
