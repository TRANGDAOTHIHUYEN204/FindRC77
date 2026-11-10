// JMultiPortraitLayout.h
// JPortraitLayout's look for quitType "inter2" (JInterClose2 / multi-ad):
// full-width quit panel on top plus an "N of M" label next to the Ad badge.
// Kept separate so the existing portrait layout is untouched.

#import "JBaseNativeLayout.h"

NS_ASSUME_NONNULL_BEGIN

@interface JMultiPortraitLayout : JBaseNativeLayout

- (void)setAdSlotCounterText:(nullable NSString *)text;

@end

NS_ASSUME_NONNULL_END
