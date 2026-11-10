#import <UIKit/UIKit.h>
#import "JQuitMethod.h"

@class JNativeOverlay;

// Mirrors Android's InterClose2 (quitType == "inter2").
@interface JInterClose2 : NSObject <JQuitMethod>

- (instancetype)initWithNativeOverlay:(JNativeOverlay *)overlay
                        quitContainer:(UIView *)container;

- (void)setTotalAdsInSequence:(NSInteger)count;
// 1-based position of the ad on screen; set right before onShow advances.
- (void)setActiveSlot:(NSInteger)index;
// Re-splits the bar when a not-yet-reached slot's fate changes, without
// restarting the current segment or countdown like onShow would.
- (void)refreshSequenceLength:(NSInteger)count;

@end
