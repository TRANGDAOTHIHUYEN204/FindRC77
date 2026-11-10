#ifndef JNativeAdLayout_h
#define JNativeAdLayout_h

#import <UIKit/UIKit.h>
#import <GoogleMobileAds/GoogleMobileAds.h>

@protocol JNativeAdLayout <NSObject>

- (GADNativeAdView *)getAdView;
- (UIView *)getQuitContainer;
- (void)renderWithNativeAd:(GADNativeAd *)nativeAd;
- (void)destroy;
- (void)show;
- (void)hide;

@optional
// "N of M" label next to the Ad badge - multi-ad layouts only; nil hides it.
- (void)setAdSlotCounterText:(NSString *)text;

@end

#endif
