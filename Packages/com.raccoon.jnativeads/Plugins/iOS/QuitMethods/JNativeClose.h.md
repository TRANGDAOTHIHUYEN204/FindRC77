# JNativeClose.h

## Tổng quan
`JNativeClose` implements `JQuitMethod` protocol với style đơn giản. Close button luôn hiển thị ngay lập tức, không có countdown.

## Import
```objective-c
#import <UIKit/UIKit.h>
#import "JQuitMethod.h"
```

## Interface
```objective-c
@interface JNativeClose : NSObject <JQuitMethod>

- (instancetype)initWithNativeOverlay:(JNativeOverlay *)overlay
                        quitContainer:(UIView *)container;

@end
```

## Initialization

| Parameter | Type | Description |
|-----------|------|-------------|
| `overlay` | `JNativeOverlay *` | Reference đến JNativeOverlay để gọi hide |
| `container` | `UIView *` | Container view để thêm quit button vào |

## Behavior

### Simple Flow
```
onShow() called
     │
     ▼
┌────────────┐
│ Show close │  Close button visible và enabled ngay lập tức
│ button     │
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
┌────┐
│ ✕  │  ← Circular close button
└────┘
```

## Visual Design
| Element | Style |
|---------|-------|
| Shape | Circular |
| Background | Semi-transparent black |
| Icon | White "✕" |
| Size | Matches container size |
| State | Always enabled |

## Differences from JInterClose

| Aspect | JInterClose | JNativeClose |
|--------|-------------|--------------|
| Countdown | 5 seconds | None |
| Initial State | Button disabled | Button enabled |
| Label | "Close in Xs" | No label |
| Layout | Wide with label | Compact button only |

## Usage
Sử dụng khi muốn user có thể đóng ad ngay lập tức:
```objective-c
quitType = @"native";
```

## Xem thêm
- [JNativeClose.m](JNativeClose.m) - Implementation
- [JQuitMethod.h.md](../Protocols/JQuitMethod.h.md) - Protocol definition
- [JInterClose.h.md](JInterClose.h.md) - Alternative: countdown close
