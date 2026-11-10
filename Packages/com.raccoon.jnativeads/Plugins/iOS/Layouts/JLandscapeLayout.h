// JLandscapeLayout.h
// Medium Template (Landscape) layout - similar to GADTMediumTemplateView in landscape mode
// Horizontal layout: Media on left (45%), Icon + Text + CTA on right (55%)
// Recommended size: 480x150 or similar landscape aspect ratio

#import "JBaseNativeLayout.h"

NS_ASSUME_NONNULL_BEGIN

/// Medium Template (Landscape) - A larger template with media on the left side.
/// Layout structure:
/// [Media View - Left 45%] | [Ad Badge]
///                          | [Icon] [Headline/Body]
///                          | [Advertiser/Store]
///                          | [CTA Button]
/// This template displays video/image media content on the left side.
@interface JLandscapeLayout : JBaseNativeLayout

@end

NS_ASSUME_NONNULL_END
