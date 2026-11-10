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
- (void)onAdPaidWithValue:(GADAdValue *)value;

@end

#endif