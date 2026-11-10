#import "JNativeOverlay.h"
#import "JNativeConstants.h"
#import "JNativeData.h"
#import "Layouts/JBannerLayout.h"
#import "Layouts/JLandscapeLayout.h"
#import "Layouts/JMultiLandscapeLayout.h"
#import "Layouts/JMultiPortraitLayout.h"
#import "Layouts/JPortraitLayout.h"
#import "QuitMethods/JInterClose.h"
#import "QuitMethods/JInterClose2.h"
#import "QuitMethods/JNativeClose.h"
#import "QuitMethods/JNoneClose.h"

@interface JNativeOverlay ()

@property(nonatomic, weak) id<JNativeOverlayCallback> callback;
@property(nonatomic, copy) NSString *unitID;
@property(nonatomic, assign, readwrite) NSTimeInterval refreshInterval;
@property(nonatomic, assign, readwrite) CGFloat x;
@property(nonatomic, assign, readwrite) CGFloat y;
@property(nonatomic, assign, readwrite) CGFloat width;
@property(nonatomic, assign, readwrite) CGFloat height;
@property(nonatomic, copy, readwrite) NSString *templateName;
@property(nonatomic, copy, readwrite) NSString *quitType;
@property(nonatomic, assign, readwrite) CGFloat paddingStart;
@property(nonatomic, assign, readwrite) CGFloat paddingEnd;
@property(nonatomic, assign, readwrite) CGFloat paddingTop;
@property(nonatomic, assign, readwrite) CGFloat paddingBottom;

@property(nonatomic, strong) GADAdLoader *adLoader;
@property(nonatomic, strong) GADNativeAd *nativeAd;
@property(nonatomic, strong) id<JNativeAdLayout> layout;
@property(nonatomic, strong) id<JQuitMethod> quitMethod;
@property(nonatomic, assign) CGFloat quitButtonScale;
@property(nonatomic, strong) NSTimer *refreshTimer;
// One-shot reload-on-click timer - independent of refreshTimer/
// kJNativeAutoRefreshEnabled.
@property(nonatomic, strong) NSTimer *clickReloadTimer;
@property(nonatomic, assign) BOOL isShowing;
@property(nonatomic, assign) BOOL isPaused;
// True once the SDK records an impression on the current ad; gates
// click-reload. Reset on load success only, not failure.
@property(nonatomic, assign) BOOL hasImpression;
// Guards against overlapping GADAdLoader requests from refresh/click-reload/
// the public Load() entry point.
@property(nonatomic, assign) BOOL isLoading;

// ---- Multi-ad (quitType "inter2" + unitID "id1_id2[_id3]") ----
// Only active with exactly 2-3 IDs; a single unitID keeps the original flow
// above (self.nativeAd/self.adLoader) untouched.
@property(nonatomic, copy) NSArray<NSString *> *multiAdUnitIds;
@property(nonatomic, assign) BOOL isMultiAd;
// slotData[i] is slot (i + 1). A late slot-1 arrival can append beyond the
// requested count - see multiAdPrimaryLoaded:.
@property(nonatomic, strong) NSMutableArray<JNativeData *> *slotData;
// 1-based slot on screen / slot the sequence started from (normally 1, later
// if slot 1 wasn't ready in time).
@property(nonatomic, assign) NSInteger currentAdSlot;
@property(nonatomic, assign) NSInteger firstShownSlot;
@property(nonatomic, assign) BOOL hasReportedLoadResult;
@property(nonatomic, assign) NSInteger slotsAttempted;
// Show() arrived before any slot loaded - shown once one does.
@property(nonatomic, assign) BOOL pendingShow;
// Set by an ad click; the return from the clicked page advances the sequence.
@property(nonatomic, assign) BOOL pendingAdvanceOnResume;
// The click opened an in-app screen (SFSafariViewController/store sheet) -
// that doesn't resign the app, so its dismissal is the "back" signal.
@property(nonatomic, assign) BOOL isPresentingAdScreen;
@property(nonatomic, strong) NSTimer *clickAdvanceFallbackTimer;
@property(nonatomic, assign) BOOL isDestroyed;

@end

@implementation JNativeOverlay

#pragma mark - Initialization

- (instancetype)initWithCallback:(id<JNativeOverlayCallback>)callback
                          unitID:(NSString *)unitID
                         refresh:(NSTimeInterval)refresh
                               x:(CGFloat)x
                               y:(CGFloat)y
                           width:(CGFloat)width
                          height:(CGFloat)height
                        template:(NSString *)templateName
                        quitType:(NSString *)quitType
                    paddingStart:(CGFloat)paddingStart
                      paddingEnd:(CGFloat)paddingEnd
                      paddingTop:(CGFloat)paddingTop
                   paddingBottom:(CGFloat)paddingBottom {
  self = [super init];
  if (self) {
    _callback = callback;
    _unitID = [unitID copy];
    _refreshInterval = refresh;
    _x = x;
    _y = y;
    _width = width;
    _height = height;
    _templateName = [templateName copy];
    _quitType = [quitType copy];
    _paddingStart = paddingStart;
    _paddingEnd = paddingEnd;
    _paddingTop = paddingTop;
    _paddingBottom = paddingBottom;
    _isShowing = NO;
    _isPaused = NO;
    _quitButtonScale = 0.35;

    [self setupMultiAd];

    // Native observers as the primary pause/resume signal - Unity's own
    // MonoBehaviour.OnApplicationPause bridge call (see onApplicationPause:
    // below) lags behind the real iOS transition by at least one engine
    // frame, which is enough time for InterClose's wall-clock-based loading
    // animation to finish (or get stuck) before the freeze is even applied.
    // WillResignActive/DidBecomeActive (rather than the background/
    // foreground pair) also catch in-app modal interruptions - e.g. an ad
    // click opening SKStoreProductViewController - which resign/reactivate
    // the app without ever truly backgrounding it.
    [[NSNotificationCenter defaultCenter] addObserver:self
                                              selector:@selector(handleAppWillResignActive)
                                                  name:UIApplicationWillResignActiveNotification
                                                object:nil];
    [[NSNotificationCenter defaultCenter] addObserver:self
                                              selector:@selector(handleAppDidBecomeActive)
                                                  name:UIApplicationDidBecomeActiveNotification
                                                object:nil];
  }
  return self;
}

- (void)setupMultiAd {
  NSMutableArray<NSString *> *ids = [NSMutableArray array];
  if ([self isInter2QuitType]) {
    for (NSString *part in [self.unitID componentsSeparatedByString:@"_"]) {
      NSString *trimmed =
          [part stringByTrimmingCharactersInSet:[NSCharacterSet whitespaceAndNewlineCharacterSet]];
      if (trimmed.length > 0) [ids addObject:trimmed];
    }
  }
  self.isMultiAd = ids.count >= 2 && ids.count <= 3;
  self.multiAdUnitIds = self.isMultiAd ? ids : @[];

  self.slotData = [NSMutableArray array];
  NSInteger slots = self.isMultiAd ? (NSInteger)ids.count : 1;
  for (NSInteger slot = 1; slot <= slots; slot++) {
    [self.slotData addObject:[[JNativeData alloc] initWithSlot:slot]];
  }
  self.currentAdSlot = 1;
  self.firstShownSlot = 1;
}

- (BOOL)isInter2QuitType {
  return [self.quitType.lowercaseString isEqualToString:@"inter2"];
}

- (void)dealloc {
  [[NSNotificationCenter defaultCenter] removeObserver:self];
}

- (void)handleAppWillResignActive {
  [self onApplicationPause:YES];
}

- (void)handleAppDidBecomeActive {
  [self onApplicationPause:NO];
}

#pragma mark - Public Methods

- (void)load {
  dispatch_async(dispatch_get_main_queue(), ^{
    if (self.isMultiAd) {
      [self loadMultiAd];
      return;
    }

    if (self.isLoading)
      return;
    self.isLoading = YES;

    [self setupAdLoader];
    [self.adLoader loadRequest:[GADRequest request]];
  });
}

- (void)show {
  if (self.isMultiAd) {
    dispatch_async(dispatch_get_main_queue(), ^{
      [self showMultiAdRequested];
    });
    return;
  }

  if (!self.nativeAd) {
    return;
  }

  dispatch_async(dispatch_get_main_queue(), ^{
    if (!self.layout) {
      [self createLayout];
    }

    if (self.nativeAd) {
      [self.layout renderWithNativeAd:self.nativeAd];
    }

    [self.layout show];

    // Refresh re-fires onLoadSucceed, and callers often call Show() again on
    // it - only run quit-button setup on the real first show, or the
    // quit/countdown UI resets every reload.
    BOOL isFirstShow = !self.isShowing;
    if (self.quitMethod && isFirstShow) {
      [self.quitMethod onShow];
    }

    self.isShowing = YES;

    if ([self.callback respondsToSelector:@selector(onImpression)]) {
      [self.callback onImpression];
    }

    [self startRefreshTimerIfNeeded];
  });
}

- (void)hide {
  dispatch_async(dispatch_get_main_queue(), ^{
    self.isShowing = NO;

    [self.layout hide];
    [self stopRefreshTimer];
    [self.clickReloadTimer invalidate];
    self.clickReloadTimer = nil;

    [self cancelClickAdvanceFallback];
    // A Show() still waiting on a load must not pop up after Hide().
    self.pendingShow = NO;
    self.pendingAdvanceOnResume = NO;
    self.isPresentingAdScreen = NO;

    if ([self.callback respondsToSelector:@selector(onClosed)]) {
      [self.callback onClosed];
    }
  });
}

- (void)destroy {
  dispatch_async(dispatch_get_main_queue(), ^{
    // In-flight multi-ad responses check this so they can't resurrect a slot.
    self.isDestroyed = YES;
    [self stopRefreshTimer];
    [self.clickReloadTimer invalidate];
    self.clickReloadTimer = nil;
    [self cancelClickAdvanceFallback];

    if (self.quitMethod) {
      [self.quitMethod destroy];
      self.quitMethod = nil;
    }

    if (self.layout) {
      [self.layout destroy];
      self.layout = nil;
    }

    self.nativeAd.delegate = nil;
    self.nativeAd = nil;
    self.adLoader = nil;
    for (JNativeData *data in self.slotData) {
      [data forceDestroy];
      data.adLoader = nil;
    }
    self.isShowing = NO;
    self.isLoading = NO;
    self.pendingShow = NO;
  });
}

- (void)onApplicationPause:(BOOL)pause {
  self.isPaused = pause;

  if (self.quitMethod &&
      [self.quitMethod respondsToSelector:@selector(onPause:)]) {
    [self.quitMethod onPause:pause];
  }

  if (self.isMultiAd) {
    if (pause) {
      // The click did open something - the fallback would otherwise advance
      // while the user is away, then again on resume.
      [self cancelClickAdvanceFallback];
    } else if (self.isShowing && self.pendingAdvanceOnResume && !self.isPresentingAdScreen) {
      [self onQuitRequested];
    }
    return;
  }

  if (pause) {
    [self stopRefreshTimer];
  } else if (self.isShowing) {
    [self startRefreshTimerIfNeeded];
  }
}

- (void)setQuitButtonScale:(CGFloat)scale {
  CGFloat clampedScale = scale > 0 ? scale : 1.0;

  // Assign the ivar directly - self.quitButtonScale = ... would recurse
  // into this setter. Set synchronously so createLayout (from Show()'s own
  // async block) reliably sees the latest value regardless of block
  // interleaving.
  _quitButtonScale = clampedScale;

  dispatch_async(dispatch_get_main_queue(), ^{
    if (self.quitMethod &&
        [self.quitMethod respondsToSelector:@selector(setButtonScale:)]) {
      [self.quitMethod setButtonScale:self.quitButtonScale];
    }
  });
}

- (void)setBannerTheme:(NSString *)theme {
  // Banner theming is an Android-only concept today (see
  // NativeOverlay.SetBannerTheme, gated behind #if UNITY_ANDROID); no themed
  // banner layout exists on iOS yet.
}

- (void)setRefreshTime:(NSTimeInterval)seconds {
  self.refreshInterval = seconds;

  if (self.refreshTimer) {
    [self stopRefreshTimer];
    if (self.isShowing && !self.isPaused) {
      [self startRefreshTimerIfNeeded];
    }
  }
}

- (void)onQuitRequested {
  // Cleared here too so an X-button advance after a click that never left
  // the app can't leave this stuck and fire a spurious extra advance.
  self.pendingAdvanceOnResume = NO;
  [self cancelClickAdvanceFallback];
  if (!self.isMultiAd) {
    [self hide];
    return;
  }
  [self advanceToNextAdOrClose];
}

#pragma mark - Private Methods

- (GADAdLoader *)makeAdLoaderWithUnitID:(NSString *)unitID {
  GADVideoOptions *videoOptions = [[GADVideoOptions alloc] init];
  // Unity no longer sends a muted flag - default muted, matching Android's
  // hardcoded VideoOptions.setStartMuted(true).
  videoOptions.startMuted = YES;

  UIViewController *rootVC = [self getRootViewController];

  GADAdLoader *loader =
      [[GADAdLoader alloc] initWithAdUnitID:unitID
                         rootViewController:rootVC
                                    adTypes:@[ GADAdLoaderAdTypeNative ]
                                    options:@[ videoOptions ]];
  loader.delegate = self;
  return loader;
}

- (void)setupAdLoader {
  self.adLoader = [self makeAdLoaderWithUnitID:self.unitID];
}

- (UIViewController *)getRootViewController {
  UIWindow *keyWindow = nil;

  if (@available(iOS 13.0, *)) {
    for (UIWindowScene *scene in [UIApplication sharedApplication]
             .connectedScenes) {
      if (scene.activationState == UISceneActivationStateForegroundActive) {
        for (UIWindow *window in scene.windows) {
          if (window.isKeyWindow) {
            keyWindow = window;
            break;
          }
        }
      }
    }
  } else {
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
    keyWindow = [UIApplication sharedApplication].keyWindow;
#pragma clang diagnostic pop
  }

  return keyWindow.rootViewController;
}

- (void)createLayout {
  UIViewController *rootVC = [self getRootViewController];
  UIView *parentView = rootVC.view;

  // Unity sends exactly one of "banner" / "landscape" / "portrait".
  NSString *lowerTemplateName = self.templateName.lowercaseString;
  BOOL isBanner = [lowerTemplateName isEqualToString:@"banner"];
  BOOL isLandscape = [lowerTemplateName isEqualToString:@"landscape"];
  // "inter2" gets its own layouts so the existing ones stay untouched.
  BOOL useInterClose2 = [self isInter2QuitType] && !isBanner;

  if (isBanner) {
    self.layout = [[JBannerLayout alloc] initWithNativeOverlay:self
                                                    parentView:parentView];
  } else if (useInterClose2 && isLandscape) {
    self.layout = [[JMultiLandscapeLayout alloc] initWithNativeOverlay:self
                                                            parentView:parentView];
  } else if (useInterClose2) {
    self.layout = [[JMultiPortraitLayout alloc] initWithNativeOverlay:self
                                                           parentView:parentView];
  } else if (isLandscape) {
    self.layout = [[JLandscapeLayout alloc] initWithNativeOverlay:self
                                                       parentView:parentView];
  } else {
    // "portrait", or any unrecognized value.
    self.layout = [[JPortraitLayout alloc] initWithNativeOverlay:self
                                                      parentView:parentView];
  }

  // Create quit method
  // "inter3" is treated the same as "inter" - JInterClose is the
  // InterClose3-style panel; "inter2" is JInterClose2 (InterClose2-style).
  UIView *quitContainer = [self.layout getQuitContainer];
  if ([self.quitType.lowercaseString isEqualToString:@"inter"] ||
      [self.quitType.lowercaseString isEqualToString:@"inter3"]) {
    self.quitMethod =
        [[JInterClose alloc] initWithNativeOverlay:self
                                     quitContainer:quitContainer
                                       buttonScale:self.quitButtonScale];
  } else if (useInterClose2) {
    self.quitMethod = [[JInterClose2 alloc] initWithNativeOverlay:self
                                                    quitContainer:quitContainer];
  } else if ([self.quitType.lowercaseString isEqualToString:@"native"]) {
    self.quitMethod =
        [[JNativeClose alloc] initWithNativeOverlay:self
                                      quitContainer:quitContainer
                                        buttonScale:self.quitButtonScale];
  } else {
    self.quitMethod = [[JNoneClose alloc] init];
  }
}

- (void)startRefreshTimerIfNeeded {
  if (!kJNativeAutoRefreshEnabled)
    return;
  // Refresh always reloads slot 1 - mid-sequence it would replace ad2/ad3.
  if (self.isMultiAd)
    return;

  if (self.refreshInterval > 0 && !self.refreshTimer) {
    self.refreshTimer =
        [NSTimer scheduledTimerWithTimeInterval:self.refreshInterval
                                         target:self
                                       selector:@selector(refreshTimerFired)
                                       userInfo:nil
                                        repeats:NO];
  }
}

- (void)stopRefreshTimer {
  if (self.refreshTimer) {
    [self.refreshTimer invalidate];
    self.refreshTimer = nil;
  }
}

- (void)refreshTimerFired {
  self.refreshTimer = nil;

  if (self.isShowing && !self.isPaused) {
    [self load];
  }
}

- (void)attachDelegateAndPaidHandlerToNativeAd:(GADNativeAd *)nativeAd {
  nativeAd.delegate = self;
  __weak typeof(self) weakSelf = self;
  nativeAd.paidEventHandler = ^(GADAdValue *value) {
    if ([weakSelf.callback respondsToSelector:@selector(onAdPaidWithValue:)]) {
      [weakSelf.callback onAdPaidWithValue:value];
    }
  };
}

#pragma mark - Multi-ad

- (void)loadMultiAd {
  if (self.isDestroyed || self.isLoading) return;
  // Mid-sequence - a fresh cycle here would race with whatever's showing.
  if (self.isShowing) return;
  JNativeData *first = self.slotData[0];
  // A ready, unseen ad is already waiting - don't discard it.
  if (first.isReady && !first.isShown) return;
  self.isLoading = YES;

  self.hasReportedLoadResult = NO;
  self.slotsAttempted = 0;
  self.firstShownSlot = 1;
  // hide() never resets this - stale here would make an on-time slot-1 load
  // look like a late arrival.
  self.currentAdSlot = 1;
  while (self.slotData.count > self.multiAdUnitIds.count) {
    [self.slotData.lastObject forceDestroy];
    [self.slotData removeLastObject];
  }
  for (JNativeData *data in self.slotData) {
    [data resetForNewCycle];
  }

  // All slots load in parallel, not one after another.
  for (NSInteger slot = 2; slot <= (NSInteger)self.multiAdUnitIds.count; slot++) {
    [self loadUpcomingAdForSlot:slot];
  }

  self.adLoader = [self makeAdLoaderWithUnitID:self.multiAdUnitIds[0]];
  [self.adLoader loadRequest:[GADRequest request]];
}

- (void)loadUpcomingAdForSlot:(NSInteger)slot {
  NSInteger index = slot - 1;
  if (index < 0 || index >= (NSInteger)self.slotData.count || self.isDestroyed) return;
  JNativeData *data = self.slotData[index];
  if (data.isLoading) return;
  // Still holding an ad from last cycle (e.g. shown without an impression) -
  // counts as this cycle's result, or the failure count could never complete.
  if (data.isReady) {
    [self onSlotLoaded];
    return;
  }
  [data onLoading];
  data.adLoader = [self makeAdLoaderWithUnitID:self.multiAdUnitIds[index]];
  [data.adLoader loadRequest:[GADRequest request]];
}

- (void)multiAdPrimaryLoaded:(GADNativeAd *)nativeAd {
  self.isLoading = NO;
  if (self.isDestroyed) return;
  [self attachDelegateAndPaidHandlerToNativeAd:nativeAd];

  if (self.currentAdSlot != 1) {
    // Lost the race - slot 1 now belongs to whatever's on screen, so queue
    // this one as a new slot at the end instead.
    JNativeData *newSlot = [[JNativeData alloc] initWithSlot:self.slotData.count + 1];
    [newSlot onLoaded:nativeAd];
    [self.slotData addObject:newSlot];
    if (self.isShowing) {
      [[self interClose2] refreshSequenceLength:[self actualSequenceLength]];
      [self updateAdSlotLabel];
    }
  } else {
    // Load only runs while not showing, so the old ad is never live.
    [self.slotData[0] forceDestroy];
    [self.slotData[0] onLoaded:nativeAd];
  }
  [self onSlotLoaded];
}

- (void)multiAdPrimaryFailed:(NSError *)error {
  self.isLoading = NO;
  if (self.isDestroyed) return;
  [self onSlotFailed:error];
}

- (void)multiAdSlot:(JNativeData *)data loaded:(GADNativeAd *)nativeAd {
  if (self.isDestroyed) {
    [data onFailed];
    return;
  }
  [self attachDelegateAndPaidHandlerToNativeAd:nativeAd];
  [data onLoaded:nativeAd];
  [self onSlotLoaded];
}

- (void)multiAdSlot:(JNativeData *)data failed:(NSError *)error {
  [data onFailed];
  if (self.isDestroyed) return;
  // Shrinks the count right away if this slot was counted optimistically.
  if (self.isShowing) {
    [[self interClose2] refreshSequenceLength:[self actualSequenceLength]];
    [self updateAdSlotLabel];
  }
  [self onSlotFailed:error];
}

- (JNativeData *)slotDataForLoader:(GADAdLoader *)adLoader {
  for (JNativeData *data in self.slotData) {
    if (data.adLoader == adLoader) return data;
  }
  return nil;
}

- (JNativeData *)slotDataForNativeAd:(GADNativeAd *)nativeAd {
  for (JNativeData *data in self.slotData) {
    if (data.nativeAd == nativeAd) return data;
  }
  return nil;
}

// Reports readiness the moment ANY slot loads - the others keep loading.
- (void)onSlotLoaded {
  self.slotsAttempted++;
  if (self.hasReportedLoadResult) return;
  self.hasReportedLoadResult = YES;
  if ([self.callback respondsToSelector:@selector(onLoadSucceed)]) {
    [self.callback onLoadSucceed];
  }
  if (self.pendingShow) {
    self.pendingShow = NO;
    [self showMultiAd];
  }
}

// Fails only once every requested slot had its attempt and none succeeded.
- (void)onSlotFailed:(NSError *)error {
  self.slotsAttempted++;
  if (self.hasReportedLoadResult) return;
  if (self.slotsAttempted >= (NSInteger)self.multiAdUnitIds.count) {
    self.hasReportedLoadResult = YES;
    if ([self.callback respondsToSelector:@selector(onLoadFailedWithError:)]) {
      [self.callback onLoadFailedWithError:error];
    }
  }
}

- (void)showMultiAdRequested {
  if (self.isDestroyed || self.isShowing) return;
  if ([self findReadySlotFrom:1] == 0) {
    self.pendingShow = YES;
    return;
  }
  self.pendingShow = NO;
  [self showMultiAd];
}

// Shows whichever slot (from 1) is ready right now - a slot not ready at
// this exact moment is skipped for good, even if it loads later.
- (void)showMultiAd {
  NSInteger slot = [self findReadySlotFrom:1];
  if (slot == 0) return;
  JNativeData *data = self.slotData[slot - 1];
  self.currentAdSlot = slot;
  self.firstShownSlot = slot;

  if (!self.layout) {
    [self createLayout];
  }

  self.isShowing = YES;
  data.isShown = YES;
  [self.layout renderWithNativeAd:data.nativeAd];
  [self.layout show];
  [self syncSequenceUI];
  [self.quitMethod onShow];
}

- (void)advanceToNextAdOrClose {
  NSInteger nextSlot = [self findReadySlotFrom:self.currentAdSlot + 1];
  if (nextSlot == 0) {
    [self hide];
    return;
  }
  JNativeData *next = self.slotData[nextSlot - 1];
  NSInteger oldSlot = self.currentAdSlot;
  self.currentAdSlot = nextSlot;
  next.isShown = YES;
  [self.layout renderWithNativeAd:next.nativeAd];
  [self.slotData[oldSlot - 1] destroy];
  [self syncSequenceUI];
  [self.quitMethod onShow];
}

// Total re-sent on every show, not just the first: a slot counted as
// surviving earlier may have failed for real by now.
- (void)syncSequenceUI {
  JInterClose2 *interClose2 = [self interClose2];
  [interClose2 setTotalAdsInSequence:[self actualSequenceLength]];
  [interClose2 setActiveSlot:[self displayPositionForSlot:self.currentAdSlot]];
  [self updateAdSlotLabel];
}

- (JInterClose2 *)interClose2 {
  return [self.quitMethod isKindOfClass:[JInterClose2 class]] ? (JInterClose2 *)self.quitMethod
                                                              : nil;
}

- (void)updateAdSlotLabel {
  if (![self.layout respondsToSelector:@selector(setAdSlotCounterText:)]) return;
  NSString *text = [NSString stringWithFormat:@"%ld of %ld",
                                              (long)[self displayPositionForSlot:self.currentAdSlot],
                                              (long)[self actualSequenceLength]];
  [self.layout setAdSlotCounterText:text];
}

// First slot >= `from` with an ad ready right now, or 0 if none - the one
// place that decides "what's next": no waiting, no retry.
- (NSInteger)findReadySlotFrom:(NSInteger)from {
  NSInteger lastSlot = self.isMultiAd ? (NSInteger)self.slotData.count : 1;
  for (NSInteger slot = MAX(from, 1); slot <= lastSlot; slot++) {
    if (self.slotData[slot - 1].isReady) return slot;
  }
  return 0;
}

// Slot 1 is pessimistic (counts only once loaded); later slots optimistic
// (count unless known-failed, even while still loading).
- (BOOL)slotSurvived:(NSInteger)slot {
  if (slot < 1 || slot > (NSInteger)self.slotData.count) return NO;
  JNativeData *data = self.slotData[slot - 1];
  return slot == 1 ? data.hasSucceeded : !data.hasFailed;
}

- (NSInteger)actualSequenceLength {
  if (!self.isMultiAd) return 1;
  NSInteger count = 0;
  for (NSInteger slot = self.firstShownSlot; slot <= (NSInteger)self.slotData.count; slot++) {
    if ([self slotSurvived:slot]) count++;
  }
  return MAX(count, 1);
}

// e.g. if slot 2 failed, slot 3 displays as position 2.
- (NSInteger)displayPositionForSlot:(NSInteger)slot {
  NSInteger position = 0;
  for (NSInteger s = self.firstShownSlot; s <= slot; s++) {
    if ([self slotSurvived:s]) position++;
  }
  return MAX(position, 1);
}

- (void)handleMultiAdClick {
  if ([self.quitMethod respondsToSelector:@selector(onAdClicked)]) {
    [self.quitMethod onAdClicked];
  }
  self.pendingAdvanceOnResume = YES;
  [self scheduleClickAdvanceFallback];
}

// If the click never resigns the app or presents a screen, it opened
// nothing - don't wait forever for a "back" that isn't coming.
- (void)scheduleClickAdvanceFallback {
  [self cancelClickAdvanceFallback];
  __weak typeof(self) weakSelf = self;
  self.clickAdvanceFallbackTimer =
      [NSTimer scheduledTimerWithTimeInterval:kJNativeMultiAdClickAdvanceFallback
                                      repeats:NO
                                        block:^(NSTimer *_Nonnull timer) {
                                          [weakSelf clickAdvanceFallbackFired];
                                        }];
}

- (void)clickAdvanceFallbackFired {
  self.clickAdvanceFallbackTimer = nil;
  if (self.isPaused || self.isPresentingAdScreen || !self.pendingAdvanceOnResume ||
      !self.isShowing) {
    return;
  }
  if ([self findReadySlotFrom:self.currentAdSlot + 1] != 0) {
    [self onQuitRequested];
  } else if ([self.quitMethod respondsToSelector:@selector(showUnlocked)]) {
    // Last ad: closing here could race a browser/store still opening, so
    // just give the X (hidden by the click) back instead of leaving the user stuck.
    [self.quitMethod showUnlocked];
  }
}

- (void)cancelClickAdvanceFallback {
  [self.clickAdvanceFallbackTimer invalidate];
  self.clickAdvanceFallbackTimer = nil;
}

#pragma mark - GADNativeAdLoaderDelegate

- (void)adLoader:(GADAdLoader *)adLoader
    didReceiveNativeAd:(GADNativeAd *)nativeAd {

  if (self.isMultiAd) {
    if (adLoader == self.adLoader) {
      [self multiAdPrimaryLoaded:nativeAd];
    } else {
      JNativeData *data = [self slotDataForLoader:adLoader];
      if (data) [self multiAdSlot:data loaded:nativeAd];
    }
    return;
  }

  self.isLoading = NO;

  self.nativeAd.delegate = nil;
  self.nativeAd = nativeAd;
  self.hasImpression = NO;
  [self attachDelegateAndPaidHandlerToNativeAd:self.nativeAd];

  if ([self.callback respondsToSelector:@selector(onLoadSucceed)]) {
    [self.callback onLoadSucceed];
  }

  if (self.isShowing && self.layout) {
    dispatch_async(dispatch_get_main_queue(), ^{
      [self.layout renderWithNativeAd:self.nativeAd];
    });
  }
}

- (void)adLoader:(GADAdLoader *)adLoader
    didFailToReceiveAdWithError:(NSError *)error {

  if (self.isMultiAd) {
    if (adLoader == self.adLoader) {
      [self multiAdPrimaryFailed:error];
    } else {
      JNativeData *data = [self slotDataForLoader:adLoader];
      if (data) [self multiAdSlot:data failed:error];
    }
    return;
  }

  self.isLoading = NO;

  if ([self.callback respondsToSelector:@selector(onLoadFailedWithError:)]) {
    [self.callback onLoadFailedWithError:error];
  }
}

#pragma mark - GADNativeAdDelegate

- (void)nativeAdDidRecordClick:(GADNativeAd *)nativeAd {

  if ([self.callback respondsToSelector:@selector(onClicked)]) {
    [self.callback onClicked];
  }

  if (self.isMultiAd) {
    [self handleMultiAdClick];
    return;
  }

  // Reload after a short delay, gated on impression, on its own timer.
  if (!self.hasImpression)
    return;

  [self.clickReloadTimer invalidate];
  self.clickReloadTimer =
      [NSTimer scheduledTimerWithTimeInterval:kJNativeClickReloadDelay
                                       target:self
                                     selector:@selector(clickReloadTimerFired)
                                     userInfo:nil
                                      repeats:NO];
}

- (void)clickReloadTimerFired {
  self.clickReloadTimer = nil;

  if (self.isShowing && !self.isPaused) {
    [self load];
  }
}

- (void)nativeAdDidRecordImpression:(GADNativeAd *)nativeAd {
  if (self.isMultiAd) {
    // Per-ad SDK impressions (like Android) - show() doesn't fire it here.
    [self slotDataForNativeAd:nativeAd].isImpression = YES;
    if ([self.callback respondsToSelector:@selector(onImpression)]) {
      [self.callback onImpression];
    }
    return;
  }
  self.hasImpression = YES;
}

- (void)nativeAdWillPresentScreen:(GADNativeAd *)nativeAd {
  if (!self.isMultiAd) return;
  self.isPresentingAdScreen = YES;
  [self cancelClickAdvanceFallback];
}

- (void)nativeAdWillDismissScreen:(GADNativeAd *)nativeAd {
}

- (void)nativeAdDidDismissScreen:(GADNativeAd *)nativeAd {
  if (!self.isMultiAd) return;
  self.isPresentingAdScreen = NO;
  if (self.isShowing && self.pendingAdvanceOnResume && !self.isPaused) {
    [self onQuitRequested];
  }
}

@end
