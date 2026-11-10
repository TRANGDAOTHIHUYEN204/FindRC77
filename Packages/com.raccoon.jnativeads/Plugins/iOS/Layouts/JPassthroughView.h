// JPassthroughView.h
// Plain UIView whose empty area does not intercept touches - any tap that
// doesn't land on one of its own subviews falls through to whatever sits
// behind it in the view hierarchy (e.g. the native ad's clickable content).

#import <UIKit/UIKit.h>

NS_ASSUME_NONNULL_BEGIN

@interface JPassthroughView : UIView

@end

NS_ASSUME_NONNULL_END
