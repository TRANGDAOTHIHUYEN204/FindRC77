#import <Foundation/Foundation.h>
#import <GoogleMobileAds/GoogleMobileAds.h>

NS_ASSUME_NONNULL_BEGIN

// One ad slot's GADNativeAd plus its load/show/impression state - slot 1
// (always present) or slot 2..N in a multi-ad sequence. Mirrors Android's
// NativeData.kt.
@interface JNativeData : NSObject

@property(nonatomic, readonly) NSInteger slot;
@property(nonatomic, strong, readonly, nullable) GADNativeAd *nativeAd;
// GADAdLoader must stay retained until its request settles.
@property(nonatomic, strong, nullable) GADAdLoader *adLoader;
@property(nonatomic, readonly) BOOL isLoading;
@property(nonatomic, readonly) BOOL hasFailed;
// True once this slot loaded at least once this cycle - independent of
// whether nativeAd still holds it, since sequence-length counting needs it.
@property(nonatomic, readonly) BOOL hasSucceeded;
@property(nonatomic, assign) BOOL isShown;
@property(nonatomic, assign) BOOL isImpression;
@property(nonatomic, readonly) BOOL isReady;

- (instancetype)initWithSlot:(NSInteger)slot;

- (void)onLoading;
- (void)onLoaded:(GADNativeAd *)ad;
- (void)onFailed;
// Start of a fresh load cycle - a slot that failed last time gets another chance.
- (void)resetForNewCycle;
// Keeps an ad that's been shown but hasn't registered an impression yet, so
// it can be shown again instead of being discarded early.
- (void)destroy;
// Bypasses the impression guard - final teardown / replacement only.
- (void)forceDestroy;

@end

NS_ASSUME_NONNULL_END
