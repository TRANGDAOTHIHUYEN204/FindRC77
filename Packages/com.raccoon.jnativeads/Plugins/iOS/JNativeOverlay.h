#import <UIKit/UIKit.h>
#import <GoogleMobileAds/GoogleMobileAds.h>
#import "JNativeOverlayCallback.h"
#import "JNativeAdLayout.h"
#import "JQuitMethod.h"

@interface JNativeOverlay : NSObject <GADNativeAdLoaderDelegate, GADNativeAdDelegate>

@property (nonatomic, readonly) CGFloat x;
@property (nonatomic, readonly) CGFloat y;
@property (nonatomic, readonly) CGFloat width;
@property (nonatomic, readonly) CGFloat height;
@property (nonatomic, readonly) CGFloat paddingStart;
@property (nonatomic, readonly) CGFloat paddingEnd;
@property (nonatomic, readonly) CGFloat paddingTop;
@property (nonatomic, readonly) CGFloat paddingBottom;
@property (nonatomic, readonly) NSString *quitType;

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
                   paddingBottom:(CGFloat)paddingBottom;

- (void)load;
- (void)show;
- (void)hide;
- (void)destroy;
- (void)onApplicationPause:(BOOL)pause;
- (void)setQuitButtonScale:(CGFloat)scale;
- (void)setBannerTheme:(NSString *)theme;
- (void)setRefreshTime:(NSTimeInterval)seconds;
// Called by JInterClose2's X button: advances to the next ad in a multi-ad
// sequence, or hides when there's nothing left (or it's not multi-ad).
- (void)onQuitRequested;

@end