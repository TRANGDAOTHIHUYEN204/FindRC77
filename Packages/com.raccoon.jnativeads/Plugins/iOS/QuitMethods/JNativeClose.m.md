# JNativeClose.m
- [JInterClose.m.md](JInterClose.m.md) - Countdown close implementation
- [JNativeClose.h.md](JNativeClose.h.md) - Header
## Xem thêm

- `dispatch_after` block captures `self` weakly (implicit in ARC)
- `nativeOverlay` và `quitContainer` là weak references
## Memory Management

| Timer | NSTimer (repeating) | dispatch_after (one-shot) |
| Feedback | "Close in Xs" text | Visual alpha change |
| UI | Label + Button | Button only |
| Delay | 5 seconds countdown | 2 seconds simple delay |
|--------|-------------|--------------|
| Aspect | JInterClose | JNativeClose |

## Differences from JInterClose

```
}
    self.quitButton = nil;
    [self.quitButton removeFromSuperview];
- (void)destroy {
```objective-c

## destroy

```
}
    [self.nativeOverlay hide];  // Trigger hide on overlay
- (void)quitButtonTapped {
```objective-c

## quitButtonTapped

```
alpha = 0.5         alpha = 1.0
Button disabled     Button enabled        [hide] called
▼                   ▼                     ▼
│                   │                     │
0s ─────────────── 2s ─────────────── User taps
```
### Timeline

```
}
    });
        self.quitButton.alpha = 1.0;
        self.quitButton.enabled = YES;
                   dispatch_get_main_queue(), ^{
    dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(2.0 * NSEC_PER_SEC)), 
    // 2. Enable after 2 seconds
    
    self.quitButton.alpha = 0.5;
    self.quitButton.enabled = NO;
    // 1. Disable button initially (2 second delay)
- (void)onShow {
```objective-c

## onShow Implementation

```
quitButton.layer.cornerRadius = size / 2;  // Circular shape
quitButton.backgroundColor = [UIColor colorWithWhite:0 alpha:0.6];  // 60% black
quitButton.font = [UIFont boldSystemFontOfSize:16];
quitButton.titleColor = [UIColor whiteColor];
quitButton.title = @"✕";
// Button appearance
```objective-c
### Styling

```
└────┘
│ ✕  │  ← Circular button
┌────┐
```
### Visual Structure

```
                   self.quitContainer.bounds.size.height);  // ~44
CGFloat size = MIN(self.quitContainer.bounds.size.width, 
```objective-c
### Button Dimensions

## setupQuitButton

```
              └── Add to quitContainer
              ├── Create circular quitButton
              │
     └── [self setupQuitButton]
     │
     ├── Store references
     │
initWithNativeOverlay:quitContainer:
```

## Initialization Flow

```
@end
@property (nonatomic, strong) UIButton *quitButton;
@property (nonatomic, weak) UIView *quitContainer;
@property (nonatomic, weak) JNativeOverlay *nativeOverlay;
@interface JNativeClose ()
```objective-c

## Private Properties

Implementation của `JNativeClose` - Native-style close button với 2 giây delay trước khi enable.
## Tổng quan


