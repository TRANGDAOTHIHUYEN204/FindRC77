# JInterClose.h

## Tổng quan
`JInterClose` implements `JQuitMethod` protocol với style giống Interstitial ads. Close button sẽ hiển thị sau một countdown timer.

## Import
```objective-c
#import <UIKit/UIKit.h>
#import "JQuitMethod.h"
```

## Interface
```objective-c
@interface JInterClose : NSObject <JQuitMethod>

- (instancetype)initWithNativeOverlay:(JNativeOverlay *)overlay
                        quitContainer:(UIView *)container;

@end
```

## Initialization

| Parameter | Type | Description |
|-----------|------|-------------|
| `overlay` | `JNativeOverlay *` | Reference đến JNativeOverlay để gọi hide |
| `container` | `UIView *` | Container view để thêm quit layout vào |

## Behavior

### Countdown Flow
```
onShow() called
     │
     ▼
┌────────────┐
│ 5 seconds  │  "Close in 3s"
│ countdown  │  "Close in 2s"
│            │  "Close in 1s"
└────────────┘
     │
     ▼
┌────────────┐
│ Enable     │  "Close ad" + ✕ button enabled
│ quit button│
└────────────┘
     │
     ▼
User taps ✕
     │
     ▼
[nativeOverlay hide]
```

## Layout Structure
```
┌─────────────────────────────────────┐
│ Close in 3s               [✕]      │  ← Rounded background
└─────────────────────────────────────┘
   └─ countdownLabel        └─ quitButton
```

## Visual Design
| Element | Style |
|---------|-------|
| Background | Black 60% opacity, full rounded corners |
| Countdown Label | White, 12pt font, left aligned |
| Quit Button | White "✕", 14pt bold font |
| Disabled State | Button alpha 0.5, not tappable |
| Enabled State | Button alpha 1.0, tappable |

## Xem thêm
- [JInterClose.m](JInterClose.m) - Implementation
- [JQuitMethod.h.md](../Protocols/JQuitMethod.h.md) - Protocol definition
- [JNativeClose.h.md](JNativeClose.h.md) - Alternative: always visible close
