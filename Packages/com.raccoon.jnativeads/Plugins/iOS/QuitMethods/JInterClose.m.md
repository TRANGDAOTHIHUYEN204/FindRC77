# JInterClose.m

## Tổng quan
Implementation của `JInterClose` - Interstitial-style close button với countdown timer.

## Private Properties

```objective-c
@interface JInterClose ()
@property (nonatomic, weak) JNativeOverlay *nativeOverlay;
@property (nonatomic, weak) UIView *quitContainer;
@property (nonatomic, strong) UIView *quitLayout;
@property (nonatomic, strong) UIButton *quitButton;
@property (nonatomic, strong) UILabel *countdownLabel;
@property (nonatomic, strong) NSTimer *countdownTimer;
@property (nonatomic, assign) NSInteger secondsRemaining;
@end
```

## Initialization Flow

```
initWithNativeOverlay:quitContainer:
     │
     ├── Store references
     │
     └── [self setupQuitLayout]
              │
              ├── Create quitLayout (rounded background)
              ├── Create countdownLabel
              ├── Create quitButton
              └── Add to quitContainer
```

## setupQuitLayout

### Layout Dimensions
```objective-c
CGFloat containerHeight = self.quitContainer.bounds.size.height;  // ~44
CGFloat layoutWidth = 140;
```

### Visual Structure
```
┌────────────────────────────────────┐
│ Close in 3s               [✕]     │
└────────────────────────────────────┘
  └── countdownLabel (95px)  └── quitButton (buttonSize)
```

### Styling
```objective-c
// Background
quitLayout.backgroundColor = [UIColor colorWithWhite:0 alpha:0.6];  // 60% black
quitLayout.layer.cornerRadius = containerHeight / 2;  // Pill shape

// Countdown Label
countdownLabel.font = [UIFont systemFontOfSize:12];
countdownLabel.textColor = [UIColor whiteColor];

// Quit Button
quitButton.title = @"✕";
quitButton.font = [UIFont boldSystemFontOfSize:14];
```

## onShow Implementation

```objective-c
- (void)onShow {
    // 1. Disable button initially
    self.quitButton.enabled = NO;
    self.quitButton.alpha = 0.5;
    
    // 2. Set countdown (5 seconds total)
    self.secondsRemaining = 5;
    [self updateCountdownLabel];
    
    // 3. Start timer (fires every 1 second)
    self.countdownTimer = [NSTimer scheduledTimerWithTimeInterval:1.0
                                                           target:self
                                                         selector:@selector(countdownTick)
                                                         userInfo:nil
                                                          repeats:YES];
}
```

## Countdown Logic

### countdownTick
```objective-c
- (void)countdownTick {
    self.secondsRemaining--;
    
    if (self.secondsRemaining <= 0) {
        // Countdown finished
        [self.countdownTimer invalidate];
        self.countdownLabel.text = @"Close ad";
        self.quitButton.enabled = YES;
        self.quitButton.alpha = 1.0;
    } else {
        [self updateCountdownLabel];
    }
}
```

### Display Seconds Calculation
```objective-c
// Hiển thị (secondsRemaining - 2) để khớp với Android behavior
NSInteger displaySeconds = self.secondsRemaining - 2;

// secondsRemaining = 5 → display "Close in 3s"
// secondsRemaining = 4 → display "Close in 2s"
// secondsRemaining = 3 → display "Close in 1s"
// secondsRemaining = 2 → display "Close ad"
// secondsRemaining = 1 → display "Close ad"
// secondsRemaining = 0 → button enabled
```

## quitButtonTapped

```objective-c
- (void)quitButtonTapped {
    [self.nativeOverlay hide];  // Trigger hide on overlay
}
```

## destroy

```objective-c
- (void)destroy {
    // 1. Stop timer to prevent leaks
    [self.countdownTimer invalidate];
    self.countdownTimer = nil;
    
    // 2. Remove views
    [self.quitLayout removeFromSuperview];
    self.quitLayout = nil;
}
```

## Memory Management
- `nativeOverlay` và `quitContainer` là weak references
- Timer phải được invalidate trong `destroy` để tránh retain cycle

## Xem thêm
- [JInterClose.h.md](JInterClose.h.md) - Header
- [JNativeClose.m.md](JNativeClose.m.md) - Immediate close implementation

