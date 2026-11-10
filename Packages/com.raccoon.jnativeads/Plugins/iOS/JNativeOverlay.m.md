# JNativeOverlay.m

## Tổng quan
Implementation của `JNativeOverlay` - class chính quản lý Native Overlay Ads.

## Private Properties

```objective-c
@interface JNativeOverlay ()
// Configuration
@property (nonatomic, weak) id<JNativeOverlayCallback> callback;
@property (nonatomic, copy) NSString *unitID;
@property (nonatomic, assign) NSTimeInterval refreshInterval;
@property (nonatomic, copy) NSString *templateName;
@property (nonatomic, copy) NSString *orientation;
@property (nonatomic, copy) NSString *quitType;
@property (nonatomic, assign) BOOL muted;

// Layout & Position
@property (nonatomic, assign) CGFloat x, y, width, height;
@property (nonatomic, assign) CGFloat paddingStart, paddingEnd, paddingTop, paddingBottom;

// Runtime
@property (nonatomic, strong) GADAdLoader *adLoader;
@property (nonatomic, strong) GADNativeAd *nativeAd;
@property (nonatomic, strong) id<JNativeAdLayout> layout;
@property (nonatomic, strong) id<JQuitMethod> quitMethod;
@property (nonatomic, strong) NSTimer *refreshTimer;
@property (nonatomic, assign) BOOL isShowing;
@property (nonatomic, assign) BOOL isPaused;
@end
```

## Lifecycle Flow

```
┌─────────┐    ┌──────────┐    ┌──────────┐    ┌─────────┐
│  init   │───▶│   load   │───▶│   show   │───▶│  hide   │
└─────────┘    └──────────┘    └──────────┘    └─────────┘
                    │                │              │
                    ▼                ▼              │
              ┌──────────┐    ┌──────────┐         │
              │ callback │    │  layout  │         │
              │ onLoaded │    │  render  │         │
              └──────────┘    └──────────┘         │
                                   │              │
                                   ▼              ▼
                            ┌──────────┐    ┌─────────┐
                            │ refresh  │    │ destroy │
                            │  timer   │    └─────────┘
                            └──────────┘
```

## Public Methods Implementation

### load
```objective-c
- (void)load {
    dispatch_async(dispatch_get_main_queue(), ^{
        [self setupAdLoader];
        [self.adLoader loadRequest:[GADRequest request]];
    });
}
```

### show
```objective-c
- (void)show {
    if (!self.nativeAd) return;  // Phải load trước
    
    dispatch_async(dispatch_get_main_queue(), ^{
        // 1. Tạo layout nếu chưa có
        if (!self.layout) [self createLayout];
        
        // 2. Render ad vào layout
        [self.layout renderWithNativeAd:self.nativeAd];
        
        // 3. Hiển thị layout
        [self.layout show];
        
        // 4. Kích hoạt quit method
        [self.quitMethod onShow];
        
        // 5. Callback
        [self.callback onImpression];
        
        // 6. Start refresh timer
        [self startRefreshTimerIfNeeded];
    });
}
```

### hide
```objective-c
- (void)hide {
    dispatch_async(dispatch_get_main_queue(), ^{
        self.isShowing = NO;
        [self.layout hide];
        [self stopRefreshTimer];
        [self.callback onClosed];
    });
}
```

### destroy
```objective-c
- (void)destroy {
    dispatch_async(dispatch_get_main_queue(), ^{
        [self stopRefreshTimer];
        [self.quitMethod destroy];
        [self.layout destroy];
        self.nativeAd.delegate = nil;
        self.nativeAd = nil;
        self.adLoader = nil;
    });
}
```

## Private Methods

### setupAdLoader
```objective-c
- (void)setupAdLoader {
    GADVideoOptions *videoOptions = [[GADVideoOptions alloc] init];
    videoOptions.startMuted = self.muted;
    
    UIViewController *rootVC = [self getRootViewController];
    
    self.adLoader = [[GADAdLoader alloc] initWithAdUnitID:self.unitID
                                       rootViewController:rootVC
                                                  adTypes:@[GADAdLoaderAdTypeNative]
                                                  options:@[videoOptions]];
    self.adLoader.delegate = self;
}
```

### createLayout
Template selection logic:
```objective-c
- (void)createLayout {
    NSString *lowerTemplateName = self.templateName.lowercaseString;
    NSString *lowerOrientation = self.orientation.lowercaseString;
    
    if ([lowerTemplateName isEqualToString:@"small"] || 
        [lowerTemplateName isEqualToString:@"banner"]) {
        self.layout = [[JBannerLayout alloc] init...];
    } 
    else if ([lowerTemplateName isEqualToString:@"medium"]) {
        if ([lowerOrientation isEqualToString:@"landscape"]) {
            self.layout = [[JLandscapeLayout alloc] init...];
        } else {
            self.layout = [[JPortraitLayout alloc] init...];
        }
    }
    else if ([lowerOrientation isEqualToString:@"landscape"]) {
        self.layout = [[JLandscapeLayout alloc] init...];
    }
    else {
        self.layout = [[JPortraitLayout alloc] init...];  // Default
    }
    
    // Setup quit method
    UIView *quitContainer = [self.layout getQuitContainer];
    // ... create quit method based on quitType
}
```

### Refresh Timer
```objective-c
- (void)startRefreshTimerIfNeeded {
    if (self.refreshInterval > 0 && !self.refreshTimer) {
        self.refreshTimer = [NSTimer scheduledTimerWithTimeInterval:self.refreshInterval
                                                             target:self
                                                           selector:@selector(refreshTimerFired)
                                                           userInfo:nil
                                                            repeats:NO];
    }
}

- (void)refreshTimerFired {
    if (self.isShowing && !self.isPaused) {
        [self load];  // Tự động load ad mới
    }
}
```

## GADNativeAdLoaderDelegate

```objective-c
- (void)adLoader:(GADAdLoader *)adLoader didReceiveNativeAd:(GADNativeAd *)nativeAd {
    self.nativeAd = nativeAd;
    self.nativeAd.delegate = self;
    
    // Setup paid event handler
    self.nativeAd.paidEventHandler = ^(GADAdValue *value) {
        [self.callback onAdPaidWithValue:value];
    };
    
    [self.callback onLoadSucceed];
    
    // Nếu đang show, render lại với ad mới
    if (self.isShowing && self.layout) {
        [self.layout renderWithNativeAd:self.nativeAd];
    }
}

- (void)adLoader:(GADAdLoader *)adLoader didFailToReceiveAdWithError:(NSError *)error {
    [self.callback onLoadFailedWithError:error];
}
```

## GADNativeAdDelegate

```objective-c
- (void)nativeAdDidRecordClick:(GADNativeAd *)nativeAd {
    [self.callback onClicked];
}

- (void)nativeAdDidRecordImpression:(GADNativeAd *)nativeAd {
    // Logged only
}

- (void)nativeAdWillPresentScreen:(GADNativeAd *)nativeAd {
    // User về App Store hoặc website
}
```

## Thread Safety
Tất cả UI operations chạy trên main thread:
```objective-c
dispatch_async(dispatch_get_main_queue(), ^{
    // UI code
});
```

## Xem thêm
- [JNativeOverlay.h.md](JNativeOverlay.h.md) - Header
- [JNativeOverlayCallback.h.md](JNativeOverlayCallback.h.md) - Callback protocol
- [Layouts/README.md](Layouts/README.md) - Layout templates
