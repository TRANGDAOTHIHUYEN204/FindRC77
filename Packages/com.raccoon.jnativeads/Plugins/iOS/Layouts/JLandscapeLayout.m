// JLandscapeLayout.m
// Medium Template (Landscape) layout - similar to GADTMediumTemplateView in
// landscape mode Horizontal layout: Media on left, Icon + Text + CTA on right

#import "JLandscapeLayout.h"
#import "../JNativeConstants.h"
#import "JNativeOverlay.h"
#import "JPassthroughView.h"
#import <UIKit/UIKit.h>

BOOL gQuitButtonOnLeft = NO; // Shared state with JInterClose

@implementation JLandscapeLayout

- (NSString *)getTemplateTypeName {
  return @"medium";
}

- (void)setupLayout {
  // Get Unity coordinates (logical pixels)
  CGFloat unityX = self.nativeOverlay.x;
  CGFloat unityY = self.nativeOverlay.y;
  CGFloat unityWidth = self.nativeOverlay.width;
  CGFloat unityHeight = self.nativeOverlay.height;

  // Use nativeScale (not scale): Unity's Screen.width/height are backed by
  // the physical pixel buffer, which follows nativeScale. On devices with
  // Display Zoom enabled, scale != nativeScale, and dividing by scale leaves
  // the container short of the real screen bounds on all 4 edges.
  CGFloat scale = [[UIScreen mainScreen] nativeScale];

  // Convert Unity logical pixels to iOS physical points
  CGFloat x = unityX / scale;
  CGFloat y = unityY / scale;
  CGFloat width = unityWidth / scale;
  CGFloat height = unityHeight / scale;

  CGFloat paddingStart = self.nativeOverlay.paddingStart / scale;
  CGFloat paddingEnd = self.nativeOverlay.paddingEnd / scale;
  CGFloat paddingTop = self.nativeOverlay.paddingTop / scale;
  CGFloat paddingBottom = self.nativeOverlay.paddingBottom / scale;


  // Container view - medium template landscape style
  self.containerView =
      [[UIView alloc] initWithFrame:CGRectMake(x, y, width, height)];
  self.containerView.clipsToBounds = YES;
  self.containerView.backgroundColor =
      [UIColor blackColor]; // Default background

  // Native Ad View
  self.nativeAdView =
      [[GADNativeAdView alloc] initWithFrame:self.containerView.bounds];
  self.nativeAdView.autoresizingMask =
      UIViewAutoresizingFlexibleWidth | UIViewAutoresizingFlexibleHeight;
  [self.containerView addSubview:self.nativeAdView];

  // Use overlay padding settings (with fallback)
  CGFloat padding = MAX(paddingStart, 12); // Use paddingStart or min 12
  CGFloat buttonHeight = 44; // Standard CTA button height

  // ===== MEDIUM TEMPLATE LANDSCAPE LAYOUT (Android native_landscape_2.xml
  // style) =====
  // [Icon]        | [        Media        ]
  // [           ] | [Ad Badge, bottom-left of media]
  // [CTA Button]  | [Headline]
  //               | [Advertiser]
  // Left column (~30% width, full height) holds icon + CTA; media fills
  // the right side above a fixed text row (headline/advertiser) pinned to
  // the bottom.

  // Decide this show's quit-button side up front (and consume the toggle
  // now) so we know which column to reserve top space in. The quit panel is
  // always pinned to the top, alternating between top-left and top-right -
  // whichever side it lands on needs its column protected so it doesn't
  // land on top of the media.
  NSString *quitType = self.nativeOverlay.quitType;
  BOOL isInterQuit = [quitType isEqualToString:@"inter"] ||
                      [quitType isEqualToString:@"inter3"];
  CGFloat quitHeight = 36;
  if (isInterQuit) {
    // JInterClose (InterClose3-style) spans the full ad width, so it always
    // overlaps both columns at the top - unlike the old corner box, which
    // only overlapped media when quitOnLeft was true.
    // JInterClose's own scale is hardcoded too (ignoring
    // self.nativeOverlay.quitButtonScale) - see JNativeConstants.h.
    CGFloat boxSize = kJInterCloseBoxCloseBaseSize * kJInterCloseFixedButtonScale;
    quitHeight = kJInterCloseBarTopMargin + kJInterCloseBarHeight + kJInterCloseRowTopMargin +
                 MAX(boxSize, kJInterCloseOpenStoreHeight);
  }
  BOOL quitOnLeft = gQuitButtonOnLeft;
  gQuitButtonOnLeft = !gQuitButtonOnLeft;
  CGFloat topReserved = paddingTop + quitHeight + 8;
  // Media always gets protected from the quit box (same rule as before) -
  // only its column moved from left to right.
  CGFloat mediaTopPadding = isInterQuit ? topReserved : (quitOnLeft ? topReserved : 0);

  // ===== LEFT COLUMN (icon on top, CTA button at the bottom - Android's
  // bottom_bar) =====
  CGFloat leftColumnWidth = width * 0.30;
  CGFloat textRowHeight = 80; // matches Android's 80dp bottom_group

  CGFloat buttonWidth = leftColumnWidth - (padding * 2);
  CGFloat buttonY = height - buttonHeight - padding;
  CGFloat iconSize = MIN(leftColumnWidth - (padding * 2),
                         MIN(buttonY - (padding * 2), 128));
  iconSize = MAX(iconSize, 40);
  CGFloat iconX = (leftColumnWidth - iconSize) / 2.0;
  CGFloat iconY = MAX((buttonY - padding - iconSize) / 2.0, padding);

  self.iconImageView = [[UIImageView alloc]
      initWithFrame:CGRectMake(iconX, iconY, iconSize, iconSize)];
  self.iconImageView.contentMode = UIViewContentModeScaleAspectFill;
  self.iconImageView.clipsToBounds = YES;
  [self.nativeAdView addSubview:self.iconImageView];
  self.nativeAdView.iconView = self.iconImageView;

  self.callToActionButton = [[UIButton alloc]
      initWithFrame:CGRectMake(padding, buttonY, buttonWidth, buttonHeight)];
  self.callToActionButton.titleLabel.font =
      [UIFont systemFontOfSize:14 weight:UIFontWeightBold];
  [self.nativeAdView addSubview:self.callToActionButton];
  self.nativeAdView.callToActionView = self.callToActionButton;

  // ===== RIGHT SIDE: media on top, text row pinned to the bottom =====
  CGFloat contentX = leftColumnWidth;
  CGFloat contentWidth = width - leftColumnWidth - padding;
  CGFloat mediaHeight = height - mediaTopPadding - textRowHeight;

  self.mediaView = [[GADMediaView alloc]
      initWithFrame:CGRectMake(contentX, mediaTopPadding, contentWidth,
                                mediaHeight)];
  self.mediaView.contentMode = UIViewContentModeScaleAspectFill;
  self.mediaView.clipsToBounds = YES;
  [self.nativeAdView addSubview:self.mediaView];
  self.nativeAdView.mediaView = self.mediaView;

  // Ad Badge (bottom-left corner of the media block, matching Android's
  // ad_label_top_right constrained to the bottom of the media container)
  CGFloat adBadgeWidth = 20;
  CGFloat adBadgeHeight = 14;
  CGFloat adBadgeY = mediaTopPadding + mediaHeight - adBadgeHeight - 8;
  self.adBadgeLabel =
      [[UILabel alloc] initWithFrame:CGRectMake(contentX + padding, adBadgeY,
                                                adBadgeWidth, adBadgeHeight)];
  self.adBadgeLabel.text = @"Ad";
  [self.nativeAdView addSubview:self.adBadgeLabel];

  // Text row (headline + advertiser), pinned to the bottom of the right side
  CGFloat textRowY = height - textRowHeight;
  CGFloat textX = contentX + padding;
  CGFloat textWidth = contentWidth - padding;

  // Headline Label (Primary text)
  self.headlineLabel = [[UILabel alloc]
      initWithFrame:CGRectMake(textX, textRowY + 8, textWidth, 20)];
  self.headlineLabel.numberOfLines = 1;
  self.headlineLabel.font = [UIFont systemFontOfSize:16
                                              weight:UIFontWeightMedium];
  [self.nativeAdView addSubview:self.headlineLabel];
  self.nativeAdView.headlineView = self.headlineLabel;

  // Body Label (Secondary text - hidden by this template's renderWithNativeAd
  // override, kept only for base class bookkeeping)
  self.bodyLabel = [[UILabel alloc]
      initWithFrame:CGRectMake(textX, textRowY + 30, textWidth, 18)];
  self.bodyLabel.numberOfLines = 1;
  self.bodyLabel.font = [UIFont systemFontOfSize:14];
  [self.nativeAdView addSubview:self.bodyLabel];

  // Advertiser Label (directly below headline, matching Android's tightly
  // stacked 2-line bottom_group)
  self.advertiserLabel =
      [[UILabel alloc] initWithFrame:CGRectMake(textX, textRowY + 30,
                                                textWidth, 16)];
  self.advertiserLabel.numberOfLines = 1;
  self.advertiserLabel.hidden = YES;
  self.advertiserLabel.font = [UIFont systemFontOfSize:12];
  [self.nativeAdView addSubview:self.advertiserLabel];
  // advertiserView is registered dynamically in renderWithNativeAd: since
  // it shares a frame with storeLabel.

  // Star Rating / Store Labels - forced hidden in renderWithNativeAd: (only
  // headline + advertiser show, matching Android's native_landscape_2.xml).
  // Kept below advertiserLabel instead of overlapping headline/advertiser.
  CGFloat belowAdvertiserY = textRowY + 30 + 20;

  self.starRatingLabel = [[UILabel alloc]
      initWithFrame:CGRectMake(textX, belowAdvertiserY, textWidth, 18)];
  self.starRatingLabel.hidden = YES;
  self.starRatingLabel.font = [UIFont systemFontOfSize:14];
  [self.nativeAdView addSubview:self.starRatingLabel];
  // starRatingView is registered dynamically in renderWithNativeAd:.

  self.storeLabel =
      [[UILabel alloc] initWithFrame:CGRectMake(textX, belowAdvertiserY,
                                                textWidth, 16)];
  self.storeLabel.numberOfLines = 1;
  self.storeLabel.hidden = YES;
  self.storeLabel.font = [UIFont systemFontOfSize:12];
  [self.nativeAdView addSubview:self.storeLabel];
  // storeView is registered dynamically in renderWithNativeAd:.

  // Layout validation - check for potential overlaps
  CGFloat minRequiredHeight = textRowHeight + iconSize + (padding * 2);
  if (minRequiredHeight > height) {
  }

  // ===== QUIT LAYOUT CONTAINER (alternates between top right and top left)
  // ===== (side/quitHeight/quitType decided up front, see mediaTopPadding)
  CGFloat quitWidth = 150; // Default width
  CGFloat quitX;

  if (isInterQuit) {
    // Full ad width - see mediaTopPadding/contentY above for why both
    // columns already reserve room for it regardless of quitOnLeft.
    quitWidth = width - paddingStart - paddingEnd;
    quitX = paddingStart;
  } else if (quitOnLeft) {
    quitX = MAX(paddingStart, 12); // Top left - keep a minimum margin so the X button isn't flush against the screen edge
  } else {
    quitX = width - quitWidth - paddingEnd; // Top right
  }
  CGFloat quitY = paddingTop;

  // Validate
  if (!isInterQuit && (quitX < 0 || quitX + quitWidth > width)) {
    quitX = MAX(0, width - quitWidth - 10);
  }
  if (quitY < 0) {
    quitY = 10;
  }

  // Pass-through view: only the quit button/label subviews it hosts should
  // absorb taps - the empty margin around them must let clicks fall through
  // to the ad content behind, otherwise a large area of the ad becomes
  // unclickable whenever the quit panel overlaps it.
  self.quitLayoutContainer = [[JPassthroughView alloc]
      initWithFrame:CGRectMake(quitX, quitY, quitWidth, quitHeight)];
  self.quitLayoutContainer.backgroundColor =
      [[UIColor redColor] colorWithAlphaComponent:0]; // Debug
  [self.nativeAdView addSubview:self.quitLayoutContainer];


  [self.parentView addSubview:self.containerView];
  self.containerView.hidden = YES;
}

// Override applyStyles for medium template landscape specific styling
- (void)applyStyles {
  [super applyStyles];

  // Medium template fonts
  self.headlineLabel.font = [UIFont systemFontOfSize:16
                                              weight:UIFontWeightMedium];
  self.bodyLabel.font = [UIFont systemFontOfSize:14];
  self.starRatingLabel.font = [UIFont systemFontOfSize:14];
  self.advertiserLabel.font = [UIFont systemFontOfSize:12];
  self.storeLabel.font = [UIFont systemFontOfSize:12];
  self.callToActionButton.titleLabel.font =
      [UIFont systemFontOfSize:14 weight:UIFontWeightBold];

  // Custom colors (#CDCDCD)
  UIColor *customColor = [UIColor colorWithRed:205.0 / 255.0
                                         green:205.0 / 255.0
                                          blue:205.0 / 255.0
                                         alpha:1.0];
  self.adBadgeLabel.backgroundColor = customColor;
  self.headlineLabel.textColor = [UIColor whiteColor];
  self.bodyLabel.textColor = [UIColor whiteColor];

  // Corner radius
  CGFloat cornerRadius = 8.0;
  // A fullscreen container should be edge-to-edge - rounding its corners
  // clips them and reveals whatever sits behind the ad at the 4 screen
  // corners, which looks like a small gap around a "fullscreen" ad.
  CGSize screenSize = self.parentView.bounds.size;
  BOOL isFullScreen =
      fabs(self.containerView.frame.size.width - screenSize.width) < 1.0 &&
      fabs(self.containerView.frame.size.height - screenSize.height) < 1.0;
  self.containerView.layer.cornerRadius = isFullScreen ? 0 : cornerRadius;
  self.iconImageView.layer.cornerRadius = cornerRadius;
  self.callToActionButton.layer.cornerRadius = cornerRadius;

  // Same reasoning as cornerRadius above: super's applyStyles draws the
  // template's card border (default #E0E0E0, 1pt) around containerView.
  // On a fullscreen ad that border sits right at the screen edge and reads
  // as a stray white/gray line framing the whole screen.
  if (isFullScreen) {
    self.containerView.layer.borderWidth = 0;
  }

  // Media view corners - media now sits on the right side as a floating
  // block (not flush against the container's edges), so round all corners.
  self.mediaView.layer.maskedCorners =
      kCALayerMinXMinYCorner | kCALayerMinXMaxYCorner |
      kCALayerMaxXMinYCorner | kCALayerMaxXMaxYCorner;
  self.mediaView.layer.cornerRadius = cornerRadius;
}

// Only headline + advertiser show for this template - remove lines below to
// bring bodyLabel/starRatingLabel/storeLabel back.
- (void)renderWithNativeAd:(GADNativeAd *)nativeAd {
  [super renderWithNativeAd:nativeAd];
  self.bodyLabel.hidden = YES;
  self.nativeAdView.bodyView = nil;
  self.starRatingLabel.hidden = YES;
  self.nativeAdView.starRatingView = nil;
  self.storeLabel.hidden = YES;
  self.nativeAdView.storeView = nil;
}

@end
