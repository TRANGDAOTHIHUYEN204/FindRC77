# JNoneClose.m

## Tổng quan
Implementation của `JNoneClose` - No-op quit method không hiển thị close button.

## Implementation

```objective-c
#import "JNoneClose.h"

@implementation JNoneClose

- (void)onShow {
    // Không làm gì - không có close button
}

- (void)destroy {
    // Không có gì để cleanup
}

@end
```

## Methods

### onShow
```objective-c
- (void)onShow {
    // No-op: Không tạo UI nào
}
```
Không làm gì cả. Ad sẽ không có close button.

### destroy
```objective-c
- (void)destroy {
    // No-op: Không có resource cần cleanup
}
```
Không có resource nào cần giải phóng.

## Use Cases

1. **Banner-style ads**: Ad hiển thị liên tục, không cần user đóng
2. **Auto-refresh ads**: Ad tự động refresh sau interval
3. **Game-controlled ads**: Game logic quyết định khi nào ẩn ad

## Example Usage

```objective-c
// Trong JNativeOverlay.m
if ([self.quitType.lowercaseString isEqualToString:@"none"]) {
    self.quitMethod = [[JNoneClose alloc] init];
}

// Ad chỉ có thể đóng bằng code:
[nativeOverlay hide];
```

## Comparison

| Class | Has UI | User Can Close | Delay |
|-------|--------|----------------|-------|
| JInterClose | ✅ Yes | ✅ After countdown | 5 seconds |
| JNativeClose | ✅ Yes | ✅ After delay | 2 seconds |
| JNoneClose | ❌ No | ❌ No | N/A |

## Xem thêm
- [JNoneClose.h.md](JNoneClose.h.md) - Header
- [JQuitMethod.h.md](../Protocols/JQuitMethod.h.md) - Protocol definition

