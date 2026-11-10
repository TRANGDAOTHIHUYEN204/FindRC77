# JNoneClose.h

## Tổng quan
`JNoneClose` implements `JQuitMethod` protocol nhưng không hiển thị close button nào. Ad chỉ có thể được đóng programmatically thông qua code.

## Import
```objective-c
#import <Foundation/Foundation.h>
#import "JQuitMethod.h"
```

## Interface
```objective-c
@interface JNoneClose : NSObject <JQuitMethod>

@end
```

## Initialization
Không cần parameters - sử dụng default initializer:
```objective-c
JNoneClose *quitMethod = [[JNoneClose alloc] init];
```

## Behavior

### No-op Flow
```
onShow() called
     │
     ▼
┌────────────┐
│ Do nothing │  Không hiển thị button nào
└────────────┘
     │
     ▼
Ad chỉ đóng khi:
- Game code gọi hide()
- Refresh timer kích hoạt
```

## Protocol Implementation

```objective-c
- (void)onShow {
    // Không làm gì - no close button
}

- (void)destroy {
    // Không có gì để cleanup
}
```

## Use Cases

1. **Banner-style ads** - Ad hiển thị liên tục, không cần close
2. **Auto-refresh ads** - Ad tự động refresh sau interval
3. **Game-controlled ads** - Game logic quyết định khi nào đóng ad

## Usage
```objective-c
quitType = @"none";
```

## Comparison

| Quit Type | Close Button | User Can Close |
|-----------|--------------|----------------|
| `inter` | After countdown | ✅ Yes |
| `native` | Immediately | ✅ Yes |
| `none` | None | ❌ No (programmatic only) |

## Xem thêm
- [JNoneClose.m](JNoneClose.m) - Implementation
- [JQuitMethod.h.md](../Protocols/JQuitMethod.h.md) - Protocol definition
- [JInterClose.h.md](JInterClose.h.md) - Countdown close
- [JNativeClose.h.md](JNativeClose.h.md) - Immediate close

