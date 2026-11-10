#import "JNativeData.h"

@interface JNativeData ()

@property(nonatomic, strong, readwrite, nullable) GADNativeAd *nativeAd;
@property(nonatomic, assign, readwrite) BOOL isLoading;
@property(nonatomic, assign, readwrite) BOOL hasFailed;
@property(nonatomic, assign, readwrite) BOOL hasSucceeded;

@end

@implementation JNativeData

- (instancetype)initWithSlot:(NSInteger)slot {
  self = [super init];
  if (self) {
    _slot = slot;
  }
  return self;
}

- (BOOL)isReady {
  return self.nativeAd != nil;
}

- (void)onLoading {
  self.isLoading = YES;
}

- (void)onLoaded:(GADNativeAd *)ad {
  self.nativeAd = ad;
  self.isLoading = NO;
  self.hasFailed = NO;
  self.hasSucceeded = YES;
  self.isShown = NO;
  self.isImpression = NO;
}

- (void)onFailed {
  self.isLoading = NO;
  self.hasFailed = YES;
}

- (void)resetForNewCycle {
  self.hasFailed = NO;
  self.hasSucceeded = NO;
}

- (void)destroy {
  if (!self.nativeAd) return;
  if (self.isShown && !self.isImpression) return;
  [self forceDestroy];
}

- (void)forceDestroy {
  self.nativeAd.delegate = nil;
  self.nativeAd.paidEventHandler = nil;
  self.nativeAd = nil;
}

@end
