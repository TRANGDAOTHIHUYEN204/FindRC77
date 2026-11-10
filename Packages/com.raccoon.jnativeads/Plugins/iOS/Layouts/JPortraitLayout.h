// JPortraitLayout.h
// Medium Template (Portrait) layout - similar to GADTMediumTemplateView
// Vertical layout: Media on top, Icon + Text in middle, CTA at bottom
// Recommended size: 320x320 or similar square/portrait aspect ratio

#import "JBaseNativeLayout.h"

NS_ASSUME_NONNULL_BEGIN

/// Medium Template (Portrait) - A larger template with prominent media display.
/// Layout structure: 
/// [Media View - Top 50%]
/// [Ad Badge] [Icon] [Headline/Body]
/// [Advertiser/Store]
/// [CTA Button - Full Width]
/// This template displays video/image media content prominently.
@interface JPortraitLayout : JBaseNativeLayout

@end

NS_ASSUME_NONNULL_END
