// JPortraitLayout.m
// Medium Template (Portrait) layout - similar to GADTMediumTemplateView
// Vertical layout: Media on top, Icon + Text in middle, CTA at bottom

#import "JPortraitLayout.h"
#import "../JNativeConstants.h"
#import "JNativeOverlay.h"
#import "JPassthroughView.h"
#import <UIKit/UIKit.h>

// Shared state with JLandscapeLayout and JInterClose
extern BOOL gQuitButtonOnLeft;

@implementation JPortraitLayout

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

  // Padding CŨNG cần chia scale (Unity gửi logical pixels)
  CGFloat paddingStart = self.nativeOverlay.paddingStart / scale;
  CGFloat paddingEnd = self.nativeOverlay.paddingEnd / scale;
  CGFloat paddingTop = self.nativeOverlay.paddingTop / scale;
  CGFloat paddingBottom = self.nativeOverlay.paddingBottom / scale;


  // Get actual screen bounds for validation
  CGRect screenBounds = [[UIScreen mainScreen] bounds];

  // Check if out of bounds
  if (y < 0) {
  }
  if (y > screenBounds.size.height) {
  }
  if (y + height > screenBounds.size.height) {
  }

  // Container view - medium template style
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

  // ===== MEDIUM TEMPLATE PORTRAIT LAYOUT =====
  // Khi full screen (height lớn), cần điều chỉnh tỷ lệ để có đủ không gian cho
  // media Media nên chiếm ~60% để hiển thị tốt hơn

  // ===== MEDIA VIEW (top section) =====
  // Detect fullscreen the same way applyStyles does, so a fullscreen ad gets
  // a square (1:1) media block instead of being stretched to fill whatever
  // height is free - growing the frame's height past its width makes the
  // box non-square, which exaggerates aspect-fill cropping and reads as a
  // "stretched/distorted" media.
  CGSize parentSize = self.parentView.bounds.size;
  BOOL isFullScreenLayout = fabs(width - parentSize.width) < 1.0 &&
                             fabs(height - parentSize.height) < 1.0;

  // Tối thiểu 120 points để đủ chỗ hiển thị
  CGFloat minMediaHeight = 120;
  CGFloat mediaWidth;
  CGFloat mediaHeight;
  CGFloat mediaX;
  CGFloat mediaY;
  if (isFullScreenLayout) {
    // Reserve the same ~180pt budget below media for icon/text/CTA as the
    // non-fullscreen branch. Width = parent width minus 10pt padding on
    // each side; height matches width for a square (1:1) block (the
    // mediaBudgetHeight clamp is just a safety fallback on unusually short
    // screens). Center the square both horizontally AND vertically within
    // that budget instead of pinning it to the top.
    CGFloat mediaBudgetHeight = height - 180;
    CGFloat mediaSize =
        MAX(MIN(width - 20, mediaBudgetHeight), minMediaHeight);
    mediaWidth = mediaSize;
    mediaHeight = mediaSize;
    mediaX = (width - mediaWidth) / 2.0;
    mediaY = MAX((mediaBudgetHeight - mediaHeight) / 2.0, 0);
  } else {
    // Tính toán media height: tối đa 60% hoặc 300 points
    CGFloat maxMediaHeight = MIN(height * 0.60, 300);
    mediaHeight = MAX(MIN(maxMediaHeight, height - 180), minMediaHeight);
    mediaWidth = width;
    mediaX = 0;
    mediaY = 0;
  }

  self.mediaView = [[GADMediaView alloc]
      initWithFrame:CGRectMake(mediaX, mediaY, mediaWidth, mediaHeight)];
  self.mediaView.contentMode = UIViewContentModeScaleAspectFill;
  self.mediaView.clipsToBounds = YES;
  [self.nativeAdView addSubview:self.mediaView];
  self.nativeAdView.mediaView = self.mediaView;

  // ===== CONTENT BAR (below media) - Android native_portrait.xml style:
  // icon spans nearly the full bar height on the left, headline/advertiser
  // stack to its right, and the CTA sits under them (from the icon's right
  // edge to the end, not full width). =====
  CGFloat contentY = mediaY + mediaHeight + padding;
  CGFloat barHeight = height - contentY;
  CGFloat contentWidth = width - (padding * 2);

  // Icon fills nearly the full bar height (Android: 128dp icon in a 140dp bar)
  CGFloat iconSize = MIN(barHeight - padding, MIN(width * 0.35, 128));
  iconSize = MAX(iconSize, 40);
  CGFloat iconY = contentY + MAX((barHeight - iconSize) / 2.0, 0);
  self.iconImageView = [[UIImageView alloc]
      initWithFrame:CGRectMake(padding, iconY, iconSize, iconSize)];
  self.iconImageView.contentMode = UIViewContentModeScaleAspectFill;
  self.iconImageView.clipsToBounds = YES;
  [self.nativeAdView addSubview:self.iconImageView];
  self.nativeAdView.iconView = self.iconImageView;

  // ===== AD BADGE (bottom-left corner of the media block, right above the
  // content bar - Android places ad_label_top_right at the bottom of the
  // media area, not above it) =====
  CGFloat adBadgeWidth = 20;
  CGFloat adBadgeHeight = 14;
  CGFloat adBadgeGap = 4;
  CGFloat adBadgeY = MAX(contentY - adBadgeGap - adBadgeHeight, mediaY);
  self.adBadgeLabel = [[UILabel alloc]
      initWithFrame:CGRectMake(mediaX + padding, adBadgeY, adBadgeWidth,
                                adBadgeHeight)];
  self.adBadgeLabel.text = @"Ad";
  [self.nativeAdView addSubview:self.adBadgeLabel];

  // Text column (right of icon)
  CGFloat textX = padding + iconSize + 10;
  CGFloat textWidth = contentWidth - iconSize - 10;

  // Headline Label (Primary text)
  self.headlineLabel = [[UILabel alloc]
      initWithFrame:CGRectMake(textX, contentY, textWidth, 20)];
  self.headlineLabel.numberOfLines = 1;
  self.headlineLabel.font = [UIFont systemFontOfSize:16
                                              weight:UIFontWeightMedium];
  [self.nativeAdView addSubview:self.headlineLabel];
  self.nativeAdView.headlineView = self.headlineLabel;

  // Body Label (Secondary text - forced hidden in renderWithNativeAd:, kept
  // only for base class bookkeeping)
  self.bodyLabel = [[UILabel alloc]
      initWithFrame:CGRectMake(textX, contentY + 22, textWidth, 18)];
  self.bodyLabel.numberOfLines = 2;
  self.bodyLabel.font = [UIFont systemFontOfSize:14];
  [self.nativeAdView addSubview:self.bodyLabel];

  // Advertiser Label (Tertiary text - directly below headline, still right
  // of the icon, matching Android's stacked headline/advertiser column)
  CGFloat tertiaryY = contentY + 22 + 20;
  self.advertiserLabel = [[UILabel alloc]
      initWithFrame:CGRectMake(textX, tertiaryY, textWidth, 16)];
  self.advertiserLabel.numberOfLines = 1;
  self.advertiserLabel.hidden = YES;
  self.advertiserLabel.font = [UIFont systemFontOfSize:12];
  [self.nativeAdView addSubview:self.advertiserLabel];
  // advertiserView is registered dynamically in renderWithNativeAd: since
  // it shares a frame with storeLabel.

  // Star Rating / Store Labels - forced hidden in renderWithNativeAd: (only
  // headline + advertiser show, matching Android's native_portrait.xml).
  // Kept below advertiserLabel instead of overlapping headline/advertiser.
  CGFloat belowAdvertiserY = tertiaryY + 20;

  self.starRatingLabel = [[UILabel alloc]
      initWithFrame:CGRectMake(textX, belowAdvertiserY, textWidth, 18)];
  self.starRatingLabel.hidden = YES;
  self.starRatingLabel.font = [UIFont systemFontOfSize:14];
  [self.nativeAdView addSubview:self.starRatingLabel];
  // starRatingView is registered dynamically in renderWithNativeAd:.

  self.storeLabel = [[UILabel alloc]
      initWithFrame:CGRectMake(textX, belowAdvertiserY, textWidth, 16)];
  self.storeLabel.numberOfLines = 1;
  self.storeLabel.hidden = YES;
  self.storeLabel.font = [UIFont systemFontOfSize:12];
  [self.nativeAdView addSubview:self.storeLabel];
  // storeView is registered dynamically in renderWithNativeAd:.

  // ===== CALL TO ACTION BUTTON (bottom of bar, right of icon only - not
  // full width, matching Android's ad_call_to_action constraint) =====
  CGFloat buttonY = height - buttonHeight - padding;
  CGFloat buttonX = textX;
  CGFloat buttonWidth = width - padding - textX;
  self.callToActionButton = [[UIButton alloc]
      initWithFrame:CGRectMake(buttonX, buttonY, buttonWidth, buttonHeight)];
  self.callToActionButton.titleLabel.font =
      [UIFont systemFontOfSize:14 weight:UIFontWeightBold];
  [self.nativeAdView addSubview:self.callToActionButton];
  self.nativeAdView.callToActionView = self.callToActionButton;

  // Layout validation - check for potential overlaps
  CGFloat minRequiredHeight = mediaHeight + iconSize + padding;
  if (minRequiredHeight > height) {
  }

  // ===== QUIT LAYOUT CONTAINER (top right corner, over media) =====
  CGFloat quitHeight = 36; // Default height
  CGFloat quitWidth = 150; // Default width

  NSString *quitType = self.nativeOverlay.quitType;
  BOOL isInterQuit = [quitType isEqualToString:@"inter"] ||
                      [quitType isEqualToString:@"inter3"];

  CGFloat quitX;
  if (isInterQuit) {
    // JInterClose (InterClose3-style) spans the full ad width: a loading bar
    // plus a close box on one side and a decoy "Open Store" pill on the
    // other, so it needs the whole row instead of a corner box.
    // JInterClose's own scale is hardcoded too (ignoring
    // self.nativeOverlay.quitButtonScale) - see JNativeConstants.h.
    CGFloat boxSize = kJInterCloseBoxCloseBaseSize * kJInterCloseFixedButtonScale;
    quitHeight = kJInterCloseBarTopMargin + kJInterCloseBarHeight + kJInterCloseRowTopMargin +
                 MAX(boxSize, kJInterCloseOpenStoreHeight);
    quitWidth = width - paddingStart - paddingEnd;
    quitX = paddingStart;
  } else if (gQuitButtonOnLeft) {
    quitX = MAX(paddingStart, 12); // Top left - keep a minimum margin so the X button isn't flush against the screen edge
  } else {
    quitX = width - quitWidth - paddingEnd; // Top right
  }
  CGFloat quitY = paddingTop;

  // Toggle position for next show (still used by the "native" quit type)
  gQuitButtonOnLeft = !gQuitButtonOnLeft;


  // Validate quit position
  if (!isInterQuit && (quitX < 0 || quitX + quitWidth > width)) {
    quitX = MAX(0, width - quitWidth - 10);
  }
  if (quitY < 0 || quitY + quitHeight > height) {
    quitY = MAX(0, 10);
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

// Override applyStyles for medium template portrait specific styling
- (void)applyStyles {
  [super applyStyles];

  // Custom colors (#CDCDCD)
  UIColor *grayColor = [UIColor colorWithRed:205.0 / 255.0
                                       green:205.0 / 255.0
                                        blue:205.0 / 255.0
                                       alpha:1.0];
  self.adBadgeLabel.backgroundColor = grayColor;
  self.headlineLabel.textColor = [UIColor whiteColor];
  self.bodyLabel.textColor = [UIColor whiteColor];

  // Medium template fonts
  self.headlineLabel.font = [UIFont systemFontOfSize:16
                                              weight:UIFontWeightMedium];
  self.bodyLabel.font = [UIFont systemFontOfSize:14];
  self.starRatingLabel.font = [UIFont systemFontOfSize:14];
  self.advertiserLabel.font = [UIFont systemFontOfSize:12];
  self.storeLabel.font = [UIFont systemFontOfSize:12];
  self.callToActionButton.titleLabel.font =
      [UIFont systemFontOfSize:14 weight:UIFontWeightBold];

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

  // Media view corners (top only)
  self.mediaView.layer.maskedCorners =
      kCALayerMinXMinYCorner | kCALayerMaxXMinYCorner;
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
