// JBannerLayout.m
// Small Template layout - similar to GADTSmallTemplateView
// Compact horizontal layout: Icon | Primary/Secondary Text | CTA Button

#import "JBannerLayout.h"
#import "JNativeOverlay.h"
#import "JPassthroughView.h"
#include <objc/objc.h>

@implementation JBannerLayout

- (NSString *)getTemplateTypeName {
  return @"small";
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

  if (y < 0) {
    y = 0;
  }
  if (y + height > screenBounds.size.height) {
  }

  // Container view - small template style (compact)
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

  // AdChoices icon pinned to top-right corner
  GADAdChoicesView *adChoicesView = [[GADAdChoicesView alloc]
      initWithFrame:CGRectMake(width - 20, 0, 20, 20)];
  self.nativeAdView.adChoicesView = adChoicesView;

  // Use overlay padding settings (with fallback to 10 for compact banner)
  CGFloat padding = MAX(paddingStart, 10);

  // Dynamic icon sizing based on height, but capped between 30-50
  CGFloat iconSize = height * 0.5; // 50% of height
  iconSize = MIN(iconSize, 50);    // Max 50
  iconSize = MAX(iconSize, 30);    // Min 30

  // ===== SMALL TEMPLATE LAYOUT: [Ad][Icon] | [Headline][Body/Store] | [CTA]
  // =====

  // Media View (hidden in small template - not registered to avoid click interference)
  self.mediaView = [[GADMediaView alloc] initWithFrame:CGRectZero];
  self.mediaView.hidden = YES;
  [self.nativeAdView addSubview:self.mediaView];

  // ===== TOP-LEFT: Ad Badge =====
  CGFloat adBadgeWidth = 18;
  CGFloat adBadgeHeight = 14;
  // Place at absolute top-left corner
  self.adBadgeLabel = [[UILabel alloc]
      initWithFrame:CGRectMake(0, 0, adBadgeWidth, adBadgeHeight)];
  self.adBadgeLabel.text = @"Ad";
  self.adBadgeLabel.font = [UIFont systemFontOfSize:9 weight:UIFontWeightBold];
  self.adBadgeLabel.textAlignment = NSTextAlignmentCenter;

  // ===== LEFT SECTION: Icon only =====
  CGFloat gapAfterIcon = 6; // Gap between icon and text
  CGFloat leftSectionWidth = padding + iconSize + gapAfterIcon;

  // Icon Image View (left, vertically centered)
  CGFloat iconY = (height - iconSize) / 2;
  self.iconImageView = [[UIImageView alloc]
      initWithFrame:CGRectMake(padding, iconY, iconSize, iconSize)];
  self.iconImageView.contentMode = UIViewContentModeScaleAspectFill;
  self.iconImageView.clipsToBounds = YES;
  [self.nativeAdView addSubview:self.iconImageView];
  self.nativeAdView.iconView = self.iconImageView;

  // ===== RIGHT SECTION: CTA Button =====
  CGFloat buttonWidth = 72;
  CGFloat buttonHeight = 28;
  CGFloat buttonX = width - buttonWidth - padding;
  CGFloat buttonY = (height - buttonHeight) / 2;

  self.callToActionButton = [[UIButton alloc]
      initWithFrame:CGRectMake(buttonX, buttonY, buttonWidth, buttonHeight)];
  self.callToActionButton.titleLabel.font =
      [UIFont systemFontOfSize:12 weight:UIFontWeightBold];
  self.callToActionButton.titleLabel.adjustsFontSizeToFitWidth = YES;
  self.callToActionButton.titleLabel.minimumScaleFactor = 0.7;
  [self.nativeAdView addSubview:self.callToActionButton];
  self.nativeAdView.callToActionView = self.callToActionButton;

  // ===== CENTER SECTION: Text (between icon and button) =====
  CGFloat textX = leftSectionWidth;
  CGFloat textWidth = buttonX - textX - padding;
  CGFloat lineHeight = 16;
  CGFloat textBlockHeight = lineHeight * 2 + 2; // 2 lines with small gap
  CGFloat textStartY = (height - textBlockHeight) / 2;

  // Headline Label (Primary text - line 1)
  self.headlineLabel = [[UILabel alloc]
      initWithFrame:CGRectMake(textX, textStartY, textWidth, lineHeight)];
  self.headlineLabel.numberOfLines = 1;
  self.headlineLabel.lineBreakMode = NSLineBreakByTruncatingTail;
  self.headlineLabel.font = [UIFont systemFontOfSize:14
                                              weight:UIFontWeightMedium];
  [self.nativeAdView addSubview:self.headlineLabel];
  self.nativeAdView.headlineView = self.headlineLabel;

  // Body Label (Secondary text - line 2)
  self.bodyLabel = [[UILabel alloc]
      initWithFrame:CGRectMake(textX, textStartY + lineHeight + 2, textWidth,
                               lineHeight)];
  self.bodyLabel.numberOfLines = 1;
  self.bodyLabel.lineBreakMode = NSLineBreakByTruncatingTail;
  self.bodyLabel.font = [UIFont systemFontOfSize:12];
  [self.nativeAdView addSubview:self.bodyLabel];
  // bodyView is registered dynamically in renderWithNativeAd: since it
  // shares a frame with starRatingLabel/storeLabel.

  // Star Rating Label (alternative to body for app ads)
  self.starRatingLabel = [[UILabel alloc]
      initWithFrame:CGRectMake(textX, textStartY + lineHeight + 2, textWidth,
                               lineHeight)];
  self.starRatingLabel.hidden = YES;
  self.starRatingLabel.font = [UIFont systemFontOfSize:12];
  [self.nativeAdView addSubview:self.starRatingLabel];
  // starRatingView is registered dynamically in renderWithNativeAd:.

  // Store Label (shown for app install ads, same position as body)
  self.storeLabel = [[UILabel alloc]
      initWithFrame:CGRectMake(textX, textStartY + lineHeight + 2, textWidth,
                               lineHeight)];
  self.storeLabel.numberOfLines = 1;
  self.storeLabel.hidden = YES;
  self.storeLabel.font = [UIFont systemFontOfSize:12];
  [self.nativeAdView addSubview:self.storeLabel];
  // storeView is registered dynamically in renderWithNativeAd:.

  // Advertiser Label (not visible in small template to save space)
  self.advertiserLabel = [[UILabel alloc] initWithFrame:CGRectZero];
  self.advertiserLabel.hidden = YES;
  [self.nativeAdView addSubview:self.advertiserLabel];
  self.nativeAdView.advertiserView = self.advertiserLabel;

  // ===== QUIT LAYOUT CONTAINER (top right corner) =====
  CGFloat quitHeight = 28; // Default for banner - compact
  CGFloat quitWidth = 28;

  // Inter quit on banner is uncommon but support it
  NSString *quitType = self.nativeOverlay.quitType;
  BOOL isInterQuit = [quitType isEqualToString:@"inter"] ||
                      [quitType isEqualToString:@"inter3"];
  if (isInterQuit) {
    quitHeight = 32;
    quitWidth = 120;
  }

  CGFloat adChoicesHeight = 20; // AdChoices icon at top-right
  CGFloat adChoicesGap = 2;     // Small gap below AdChoices

  CGFloat quitX = width - quitWidth - paddingEnd;
  CGFloat quitY = paddingTop + adChoicesHeight + adChoicesGap;


  // Validate và adjust
  if (quitX < 0 || quitX + quitWidth > width) {
    quitX = MAX(0, width - quitWidth - 4);
  }
  if (quitY < 0) {
    quitY = 4;
  }

  // Pass-through view: only the quit button/label subviews it hosts should
  // absorb taps - the empty margin around them must let clicks fall through
  // to the ad content behind, otherwise a large area of the ad becomes
  // unclickable whenever the quit panel overlaps it.
  self.quitLayoutContainer = [[JPassthroughView alloc]
      initWithFrame:CGRectMake(quitX, quitY, quitWidth, quitHeight)];
  self.quitLayoutContainer.backgroundColor =
      [[UIColor redColor] colorWithAlphaComponent:0]; // Debug
  // Must be set AFTER creation - assigning to the old quitLayoutContainer
  // reference here was a no-op since the view didn't exist yet at that point.
  self.quitLayoutContainer.hidden = !isInterQuit;
  [self.nativeAdView addSubview:self.quitLayoutContainer];


  // Add AdChoices view on top other view
  [self.nativeAdView addSubview:adChoicesView];

  // Add Ad Badge on top layer (after all other views)
  [self.nativeAdView addSubview:self.adBadgeLabel];

  [self.parentView addSubview:self.containerView];
  self.containerView.hidden = YES;
}

// Override to use smaller fonts and compact styling for small template
- (void)applyStyles {
  [super applyStyles];

  // Color #CDCDCD for Ad badge background, headline and body text
  UIColor *grayColor = [UIColor colorWithRed:0.804
                                       green:0.804
                                        blue:0.804
                                       alpha:1.0];

  // Override with compact fonts specific to small template
  self.headlineLabel.font = [UIFont systemFontOfSize:14
                                              weight:UIFontWeightMedium];
  self.headlineLabel.textColor = [UIColor whiteColor];
  self.bodyLabel.font = [UIFont systemFontOfSize:12];
  self.bodyLabel.textColor = [UIColor whiteColor];
  self.starRatingLabel.font = [UIFont systemFontOfSize:12];
  self.storeLabel.font = [UIFont systemFontOfSize:12];
  self.callToActionButton.titleLabel.font =
      [UIFont systemFontOfSize:12 weight:UIFontWeightBold];

  // Compact corner radius for small template
  CGFloat cornerRadius = 4.0;
  self.containerView.layer.cornerRadius = cornerRadius;
  self.iconImageView.layer.cornerRadius = cornerRadius;
  self.callToActionButton.layer.cornerRadius = cornerRadius;

  // Ad badge styling for small template
  self.adBadgeLabel.font = [UIFont systemFontOfSize:9
                                             weight:UIFontWeightSemibold];
  self.adBadgeLabel.textColor = [UIColor colorWithRed:0.0
                                                green:0.52
                                                 blue:0.0
                                                alpha:1.0];
  self.adBadgeLabel.backgroundColor = grayColor;
  self.adBadgeLabel.textAlignment = NSTextAlignmentCenter;
  self.adBadgeLabel.layer.borderColor = self.adBadgeLabel.textColor.CGColor;
  self.adBadgeLabel.layer.borderWidth = 0.5;
  self.adBadgeLabel.layer.cornerRadius = 2.0;
  self.adBadgeLabel.layer.masksToBounds = YES;
}

// Override renderWithNativeAd to handle small template-specific rendering
- (void)renderWithNativeAd:(GADNativeAd *)nativeAd {
  // Set headline (Primary text)
  self.headlineLabel.text = nativeAd.headline;

  // Set call to action
  [self.callToActionButton setTitle:nativeAd.callToAction
                           forState:UIControlStateNormal];

  // Set icon
  if (nativeAd.icon.image) {
    self.iconImageView.image = nativeAd.icon.image;
    self.iconImageView.hidden = NO;
  } else {
    self.iconImageView.hidden = YES;
  }

  // Handle secondary text based on ad type
  // Priority: Star Rating > Store > Body text. Only the visible label stays
  // registered as an asset view - keeping all three registered with an
  // identical overlapping frame is what triggers the SDK's "2 assets can
  // not be placed on top of another asset" validation error.
  if (nativeAd.starRating && nativeAd.starRating.floatValue > 0) {
    // App install ad with rating
    self.starRatingLabel.text = [self starStringFromRating:nativeAd.starRating];
    self.starRatingLabel.hidden = NO;
    self.nativeAdView.starRatingView = self.starRatingLabel;
    self.bodyLabel.hidden = YES;
    self.nativeAdView.bodyView = nil;
    self.storeLabel.hidden = YES;
    self.nativeAdView.storeView = nil;
  } else if (nativeAd.store && nativeAd.store.length > 0) {
    // App install ad with store name
    self.storeLabel.text = nativeAd.store;
    self.storeLabel.hidden = NO;
    self.nativeAdView.storeView = self.storeLabel;
    self.bodyLabel.hidden = YES;
    self.nativeAdView.bodyView = nil;
    self.starRatingLabel.hidden = YES;
    self.nativeAdView.starRatingView = nil;
  } else {
    // Content ad with body text
    self.bodyLabel.text = nativeAd.body;
    self.bodyLabel.hidden = NO;
    self.nativeAdView.bodyView = self.bodyLabel;
    self.starRatingLabel.hidden = YES;
    self.nativeAdView.starRatingView = nil;
    self.storeLabel.hidden = YES;
    self.nativeAdView.storeView = nil;
  }

  // No media in small template layout
  self.mediaView.hidden = YES;

  // ✅ CRITICAL: Assign nativeAd to nativeAdView to enable clicks and proper
  // rendering
  self.nativeAdView.nativeAd = nativeAd;
}

@end
