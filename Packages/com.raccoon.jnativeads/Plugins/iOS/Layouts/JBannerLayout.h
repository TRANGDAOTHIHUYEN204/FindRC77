// JBannerLayout.h
// Small Template layout - similar to GADTSmallTemplateView
// Compact horizontal layout: Icon | Primary/Secondary Text | CTA Button
// Recommended size: 320x90 (similar to banner ad size)

#import "JBaseNativeLayout.h"

NS_ASSUME_NONNULL_BEGIN

/// Small Template - A compact horizontal layout suitable for banner-like placements.
/// Layout structure: [Ad Badge][Icon] | [Headline][Body/Store/Rating] | [CTA Button]
/// This template does not display media content.
@interface JBannerLayout : JBaseNativeLayout

@end

NS_ASSUME_NONNULL_END
