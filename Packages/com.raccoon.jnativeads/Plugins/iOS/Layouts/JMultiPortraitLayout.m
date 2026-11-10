// JMultiPortraitLayout.m
// Same frame math as JPortraitLayout (media on top, icon + text + CTA below),
// with the quit container always spanning the full ad width for JInterClose2.

#import "JMultiPortraitLayout.h"
#import "../JNativeConstants.h"
#import "JNativeOverlay.h"
#import "JPassthroughView.h"
#import <UIKit/UIKit.h>

static const CGFloat kAdSlotCounterGap = 4;
static const CGFloat kAdSlotCounterHorizontalPadding = 4;

@interface JMultiPortraitLayout ()
@property(nonatomic, strong) UILabel *adSlotCounterLabel;
@end

@implementation JMultiPortraitLayout

- (NSString *)getTemplateTypeName {
  return @"medium";
}

- (void)setupLayout {
  // Unity sends physical pixels; nativeScale (not scale) matches its buffer
  // even with Display Zoom - see JPortraitLayout.
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

  // ===== MEDIA (square 1:1 block when fullscreen, see JPortraitLayout) =====
  CGSize parentSize = self.parentView.bounds.size;
  BOOL isFullScreenLayout = fabs(width - parentSize.width) < 1.0 &&
                            fabs(height - parentSize.height) < 1.0;
  CGFloat minMediaHeight = 120;
  CGFloat mediaWidth;
  CGFloat mediaHeight;
  CGFloat mediaX;
  CGFloat mediaY;
  if (isFullScreenLayout) {
    CGFloat mediaBudgetHeight = height - 180;
    CGFloat mediaSize = MAX(MIN(width - 20, mediaBudgetHeight), minMediaHeight);
    mediaWidth = mediaSize;
    mediaHeight = mediaSize;
    mediaX = (width - mediaWidth) / 2.0;
    mediaY = MAX((mediaBudgetHeight - mediaHeight) / 2.0, 0);
  } else {
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

  // ===== CONTENT BAR =====
  CGFloat contentY = mediaY + mediaHeight + padding;
  CGFloat barHeight = height - contentY;
  CGFloat contentWidth = width - (padding * 2);

  CGFloat iconSize = MIN(barHeight - padding, MIN(width * 0.35, 128));
  iconSize = MAX(iconSize, 40);
  CGFloat iconY = contentY + MAX((barHeight - iconSize) / 2.0, 0);
  self.iconImageView =
      [[UIImageView alloc] initWithFrame:CGRectMake(padding, iconY, iconSize, iconSize)];
  self.iconImageView.contentMode = UIViewContentModeScaleAspectFill;
  self.iconImageView.clipsToBounds = YES;
  [self.nativeAdView addSubview:self.iconImageView];
  self.nativeAdView.iconView = self.iconImageView;

  // ===== AD BADGE + "N of M" (bottom-left of the media block) =====
  CGFloat adBadgeWidth = 20;
  CGFloat adBadgeHeight = 14;
  CGFloat adBadgeY = MAX(contentY - 4 - adBadgeHeight, mediaY);
  self.adBadgeLabel = [[UILabel alloc]
      initWithFrame:CGRectMake(mediaX + padding, adBadgeY, adBadgeWidth, adBadgeHeight)];
  self.adBadgeLabel.text = @"Ad";
  [self.nativeAdView addSubview:self.adBadgeLabel];

  self.adSlotCounterLabel = [[UILabel alloc]
      initWithFrame:CGRectMake(CGRectGetMaxX(self.adBadgeLabel.frame) + kAdSlotCounterGap,
                               adBadgeY, 0, adBadgeHeight)];
  self.adSlotCounterLabel.hidden = YES;
  [self.nativeAdView addSubview:self.adSlotCounterLabel];

  CGFloat textX = padding + iconSize + 10;
  CGFloat textWidth = contentWidth - iconSize - 10;

  self.headlineLabel =
      [[UILabel alloc] initWithFrame:CGRectMake(textX, contentY, textWidth, 20)];
  self.headlineLabel.numberOfLines = 1;
  [self.nativeAdView addSubview:self.headlineLabel];
  self.nativeAdView.headlineView = self.headlineLabel;

  // Body/star/store are forced hidden in renderWithNativeAd:, kept only for
  // base class bookkeeping (same as JPortraitLayout).
  self.bodyLabel =
      [[UILabel alloc] initWithFrame:CGRectMake(textX, contentY + 22, textWidth, 18)];
  self.bodyLabel.numberOfLines = 2;
  [self.nativeAdView addSubview:self.bodyLabel];

  CGFloat tertiaryY = contentY + 22 + 20;
  self.advertiserLabel =
      [[UILabel alloc] initWithFrame:CGRectMake(textX, tertiaryY, textWidth, 16)];
  self.advertiserLabel.numberOfLines = 1;
  self.advertiserLabel.hidden = YES;
  [self.nativeAdView addSubview:self.advertiserLabel];

  CGFloat belowAdvertiserY = tertiaryY + 20;
  self.starRatingLabel =
      [[UILabel alloc] initWithFrame:CGRectMake(textX, belowAdvertiserY, textWidth, 18)];
  self.starRatingLabel.hidden = YES;
  [self.nativeAdView addSubview:self.starRatingLabel];

  self.storeLabel =
      [[UILabel alloc] initWithFrame:CGRectMake(textX, belowAdvertiserY, textWidth, 16)];
  self.storeLabel.numberOfLines = 1;
  self.storeLabel.hidden = YES;
  [self.nativeAdView addSubview:self.storeLabel];

  CGFloat buttonY = height - buttonHeight - padding;
  self.callToActionButton = [[UIButton alloc]
      initWithFrame:CGRectMake(textX, buttonY, width - padding - textX, buttonHeight)];
  [self.nativeAdView addSubview:self.callToActionButton];
  self.nativeAdView.callToActionView = self.callToActionButton;

  // ===== QUIT CONTAINER (full width on top, over media) =====
  CGFloat boxSize = kJInterCloseBoxCloseBaseSize * kJInterCloseFixedButtonScale;
  CGFloat quitHeight = kJInterCloseBarTopMargin + kJInterCloseBarHeight +
                       kJInterCloseRowTopMargin + MAX(boxSize, kJInterCloseOpenStoreHeight);
  CGFloat quitWidth = width - paddingStart - paddingEnd;
  CGFloat quitY = paddingTop;
  if (quitY < 0 || quitY + quitHeight > height) {
    quitY = 10;
  }

  self.quitLayoutContainer = [[JPassthroughView alloc]
      initWithFrame:CGRectMake(paddingStart, quitY, quitWidth, quitHeight)];
  self.quitLayoutContainer.backgroundColor = [UIColor clearColor];
  [self.nativeAdView addSubview:self.quitLayoutContainer];

  [self.parentView addSubview:self.containerView];
  self.containerView.hidden = YES;
}

- (void)applyStyles {
  [super applyStyles];

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

  self.headlineLabel.font = [UIFont systemFontOfSize:16 weight:UIFontWeightMedium];
  self.bodyLabel.font = [UIFont systemFontOfSize:14];
  self.starRatingLabel.font = [UIFont systemFontOfSize:14];
  self.advertiserLabel.font = [UIFont systemFontOfSize:12];
  self.storeLabel.font = [UIFont systemFontOfSize:12];
  self.callToActionButton.titleLabel.font = [UIFont systemFontOfSize:14 weight:UIFontWeightBold];

  // Fullscreen: no rounded corners/border at the screen edge (see JPortraitLayout).
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

  self.mediaView.layer.maskedCorners = kCALayerMinXMinYCorner | kCALayerMaxXMinYCorner;
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
