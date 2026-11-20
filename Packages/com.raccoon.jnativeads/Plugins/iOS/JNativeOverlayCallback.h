#ifndef JNativeOverlayCallback_h
#define JNativeOverlayCallback_h

#import <Foundation/Foundation.h>
#import <GoogleMobileAds/GoogleMobileAds.h>

@protocol JNativeOverlayCallback <NSObject>

- (void)onLoadSucceed;
- (void)onLoadFailedWithError:(NSError *)error;
- (void)onClosed;
- (void)onClicked;
- (void)onImpression;
// adSourceInfo: ad_source_name/id, instance, adapter class, mediation group/ab test (all NSString).
- (void)onAdPaidWithValue:(GADAdValue *)value
             adSourceInfo:(NSDictionary<NSString *, NSString *> *)adSourceInfo;

@end

#endif