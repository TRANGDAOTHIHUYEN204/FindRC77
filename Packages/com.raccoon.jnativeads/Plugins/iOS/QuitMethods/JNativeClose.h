#import <UIKit/UIKit.h>
#import "JQuitMethod.h"

@class JNativeOverlay;

@interface JNativeClose :  NSObject <JQuitMethod>

- (instancetype)initWithNativeOverlay:(JNativeOverlay *)overlay
                        quitContainer:(UIView *)container
                          buttonScale:(CGFloat)buttonScale;

@end