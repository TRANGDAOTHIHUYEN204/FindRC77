// JPassthroughView.m

#import "JPassthroughView.h"

@implementation JPassthroughView

- (UIView *)hitTest:(CGPoint)point withEvent:(UIEvent *)event {
  UIView *hit = [super hitTest:point withEvent:event];
  // super returns self when the point is inside our bounds but no subview
  // claimed it - treat that as "not our concern" so the touch keeps looking
  // for a target behind us instead of being swallowed here.
  return hit == self ? nil : hit;
}

@end
