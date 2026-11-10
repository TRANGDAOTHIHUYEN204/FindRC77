// JMultiLandscapeLayout.m
// Same frame math as JLandscapeLayout (icon + CTA column on the left, media
// + text on the right), with the quit container always spanning the full ad
// width for JInterClose2 and media pushed below it.

#import "JMultiLandscapeLayout.h"
#import "../JNativeConstants.h"
#import "JNativeOverlay.h"
#import "JPassthroughView.h"
#import <UIKit/UIKit.h>

static const CGFloat kAdSlotCounterGap = 4;
static const CGFloat kAdSlotCounterHorizontalPadding = 4;

@interface JMultiLandscapeLayout ()
@property(nonatomic, strong) UILabel *adSlotCounterLabel;
@end

@implementation JMultiLandscapeLayout

- (NSString *)getTemplateTypeName {
  return @"medium";
}

- (void)setupLayout {
  // Unity sends physical pixels; nativeScale (not scale) matches its buffer
  // even with Display Zoom - see JLandscapeLayout.
  CGFloat scale = [[UIScreen mainScreen] nativeScale];
  CGFloat x = self.nativeOverlay.x / scale;
  CGFloat y = self.nativeOverlay.y / scale;
  CGFloat width = self.nativeOverlay.width / scale;
  CGFloat height = self.nativeOverlay.height / scale;
  CGFloat paddingStart = self.nativeOverlay.paddingStart / scale;
  CGFloat paddingEnd = self.nativeOverlay.paddingEnd / scale;
  CGFloat paddingTop = self.nativeOverlay.paddingTop / scale;

  self.containerView = [[UIView alloc] initWithFrame:CGRectMake(x, y, width, height)];
  self.containerView.clipsToBounds = YES;
  self.containerView.backgroundColor = [UIColor blackColor];

  self.nativeAdView = [[GADNativeAdView alloc] initWithFrame:self.containerView.bounds];
  self.nativeAdView.autoresizingMask =
      UIViewAutoresizingFlexibleWidth | UIViewAutoresizingFlexibleHeight;
  [self.containerView addSubview:self.nativeAdView];

  CGFloat padding = MAX(paddingStart, 12);
  CGFloat buttonHeight = 44;

  CGFloat boxSize = kJInterCloseBoxCloseBaseSize * kJInterCloseFixedButtonScale;
  CGFloat quitHeight = kJInterCloseBarTopMargin + kJInterCloseBarHeight +
                       kJInterCloseRowTopMargin + MAX(boxSize, kJInterCloseOpenStoreHeight);
  CGFloat mediaTopPadding = paddingTop + quitHeight + 8;

  // ===== LEFT COLUMN: icon on top, CTA at the bottom =====
  CGFloat leftColumnWidth = width * 0.30;
  CGFloat textRowHeight = 80;

  CGFloat buttonWidth = leftColumnWidth - (padding * 2);
  CGFloat buttonY = height - buttonHeight - padding;
  CGFloat iconSize = MIN(leftColumnWidth - (padding * 2), MIN(buttonY - (padding * 2), 128));
  iconSize = MAX(iconSize, 40);
  CGFloat iconX = (leftColumnWidth - iconSize) / 2.0;
  CGFloat iconY = MAX((buttonY - padding - iconSize) / 2.0, padding);

  self.iconImageView =
      [[UIImageView alloc] initWithFrame:CGRectMake(iconX, iconY, iconSize, iconSize)];
  self.iconImageView.contentMode = UIViewContentModeScaleAspectFill;
  self.iconImageView.clipsToBounds = YES;
  [self.nativeAdView addSubview:self.iconImageView];
  self.nativeAdView.iconView = self.iconImageView;

  self.callToActionButton = [[UIButton alloc]
      initWithFrame:CGRectMake(padding, buttonY, buttonWidth, buttonHeight)];
  [self.nativeAdView addSubview:self.callToActionButton];
  self.nativeAdView.callToActionView = self.callToActionButton;

  // ===== RIGHT SIDE: media on top, text row pinned to the bottom =====
  CGFloat contentX = leftColumnWidth;
  CGFloat contentWidth = width - leftColumnWidth - padding;
  CGFloat mediaHeight = height - mediaTopPadding - textRowHeight;

  self.mediaView = [[GADMediaView alloc]
      initWithFrame:CGRectMake(contentX, mediaTopPadding, contentWidth, mediaHeight)];
  self.mediaView.contentMode = UIViewContentModeScaleAspectFill;
  self.mediaView.clipsToBounds = YES;
  [self.nativeAdView addSubview:self.mediaView];
  self.nativeAdView.mediaView = self.mediaView;

  // ===== AD BADGE + "N of M" (bottom-left of the media block) =====
  CGFloat adBadgeWidth = 20;
  CGFloat adBadgeHeight = 14;
  CGFloat adBadgeY = mediaTopPadding + mediaHeight - adBadgeHeight - 8;
  self.adBadgeLabel = [[UILabel alloc]
      initWithFrame:CGRectMake(contentX + padding, adBadgeY, adBadgeWidth, adBadgeHeight)];
  self.adBadgeLabel.text = @"Ad";
  [self.nativeAdView addSubview:self.adBadgeLabel];

  self.adSlotCounterLabel = [[UILabel alloc]
      initWithFrame:CGRectMake(CGRectGetMaxX(self.adBadgeLabel.frame) + kAdSlotCounterGap,
                               adBadgeY, 0, adBadgeHeight)];
  self.adSlotCounterLabel.hidden = YES;
  [self.nativeAdView addSubview:self.adSlotCounterLabel];

  CGFloat textRowY = height - textRowHeight;
  CGFloat textX = contentX + padding;
  CGFloat textWidth = contentWidth - padding;

  self.headlineLabel =
      [[UILabel alloc] initWithFrame:CGRectMake(textX, textRowY + 8, textWidth, 20)];
  self.headlineLabel.numberOfLines = 1;
  [self.nativeAdView addSubview:self.headlineLabel];
  self.nativeAdView.headlineView = self.headlineLabel;

  // Body/star/store are forced hidden in renderWithNativeAd:, kept only for
  // base class bookkeeping (same as JLandscapeLayout).
  self.bodyLabel =
      [[UILabel alloc] initWithFrame:CGRectMake(textX, textRowY + 30, textWidth, 18)];
  self.bodyLabel.numberOfLines = 1;
  [self.nativeAdView addSubview:self.bodyLabel];

  self.advertiserLabel =
      [[UILabel alloc] initWithFrame:CGRectMake(textX, textRowY + 30, textWidth, 16)];
  self.advertiserLabel.numberOfLines = 1;
  self.advertiserLabel.hidden = YES;
  [self.nativeAdView addSubview:self.advertiserLabel];

  CGFloat belowAdvertiserY = textRowY + 30 + 20;
  self.starRatingLabel =
      [[UILabel alloc] initWithFrame:CGRectMake(textX, belowAdvertiserY, textWidth, 18)];
  self.starRatingLabel.hidden = YES;
  [self.nativeAdView addSubview:self.starRatingLabel];

  self.storeLabel =
      [[UILabel alloc] initWithFrame:CGRectMake(textX, belowAdvertiserY, textWidth, 16)];
  self.storeLabel.numberOfLines = 1;
  self.storeLabel.hidden = YES;
  [self.nativeAdView addSubview:self.storeLabel];

  // ===== QUIT CONTAINER (full width on top) =====
  CGFloat quitWidth = width - paddingStart - paddingEnd;
  CGFloat quitY = paddingTop < 0 ? 10 : paddingTop;

  self.quitLayoutContainer = [[JPassthroughView alloc]
      initWithFrame:CGRectMake(paddingStart, quitY, quitWidth, quitHeight)];
  self.quitLayoutContainer.backgroundColor = [UIColor clearColor];
  [self.nativeAdView addSubview:self.quitLayoutContainer];

  [self.parentView addSubview:self.containerView];
  self.containerView.hidden = YES;
}

- (void)applyStyles {
  [super applyStyles];

  self.headlineLabel.font = [UIFont systemFontOfSize:16 weight:UIFontWeightMedium];
  self.bodyLabel.font = [UIFont systemFontOfSize:14];
  self.starRatingLabel.font = [UIFont systemFontOfSize:14];
  self.advertiserLabel.font = [UIFont systemFontOfSize:12];
  self.storeLabel.font = [UIFont systemFontOfSize:12];
  self.callToActionButton.titleLabel.font = [UIFont systemFontOfSize:14 weight:UIFontWeightBold];

  UIColor *grayColor = [UIColor colorWithRed:205.0 / 255.0
                                       green:205.0 / 255.0
                                        blue:205.0 / 255.0
                                       alpha:1.0]; // #CDCDCD
  self.adBadgeLabel.backgroundColor = grayColor;
  self.headlineLabel.textColor = [UIColor whiteColor];
  self.bodyLabel.textColor = [UIColor whiteColor];

  self.adSlotCounterLabel.backgroundColor = grayColor;
  self.adSlotCounterLabel.textColor = [UIColor blackColor];
  self.adSlotCounterLabel.font = [UIFont systemFontOfSize:10];
  self.adSlotCounterLabel.textAlignment = NSTextAlignmentCenter;

  // Fullscreen: no rounded corners/border at the screen edge (see JLandscapeLayout).
  CGFloat cornerRadius = 8.0;
  CGSize screenSize = self.parentView.bounds.size;
  BOOL isFullScreen =
      fabs(self.containerView.frame.size.width - screenSize.width) < 1.0 &&
      fabs(self.containerView.frame.size.height - screenSize.height) < 1.0;
  self.containerView.layer.cornerRadius = isFullScreen ? 0 : cornerRadius;
  if (isFullScreen) {
    self.containerView.layer.borderWidth = 0;
  }
  self.iconImageView.layer.cornerRadius = cornerRadius;
  self.callToActionButton.layer.cornerRadius = cornerRadius;

  self.mediaView.layer.maskedCorners = kCALayerMinXMinYCorner | kCALayerMinXMaxYCorner |
                                       kCALayerMaxXMinYCorner | kCALayerMaxXMaxYCorner;
  self.mediaView.layer.cornerRadius = cornerRadius;
}

- (void)renderWithNativeAd:(GADNativeAd *)nativeAd {
  [super renderWithNativeAd:nativeAd];
  self.bodyLabel.hidden = YES;
  self.nativeAdView.bodyView = nil;
  self.starRatingLabel.hidden = YES;
  self.nativeAdView.starRatingView = nil;
  self.storeLabel.hidden = YES;
  self.nativeAdView.storeView = nil;
}

- (void)setAdSlotCounterText:(NSString *)text {
  if (text.length == 0) {
    self.adSlotCounterLabel.hidden = YES;
    return;
  }
  self.adSlotCounterLabel.text = text;
  CGFloat textWidth = ceil([text sizeWithAttributes:@{
                             NSFontAttributeName : self.adSlotCounterLabel.font
                           }].width);
  CGRect frame = self.adSlotCounterLabel.frame;
  frame.size.width = textWidth + kAdSlotCounterHorizontalPadding * 2;
  self.adSlotCounterLabel.frame = frame;
  self.adSlotCounterLabel.hidden = NO;
}

@end
