// JBaseNativeLayout.m
// Base class for native ad layouts with common functionality

#import "JBaseNativeLayout.h"
#import "JNativeOverlay.h"

@implementation JBaseNativeLayout

#pragma mark - Initialization

- (instancetype)initWithNativeOverlay:(JNativeOverlay *)overlay
                           parentView:(UIView *)parentView {
    return [self initWithNativeOverlay:overlay parentView:parentView style:nil];
}

- (instancetype)initWithNativeOverlay:(JNativeOverlay *)overlay
                           parentView:(UIView *)parentView
                                style:(JNativeTemplateStyle *)style {
    self = [super init];
    if (self) {
        _nativeOverlay = overlay;
        _parentView = parentView;
        _templateStyle = style;
        [self setupLayout];
        [self applyStyles];
    }
    return self;
}

#pragma mark - Template Configuration

- (NSString *)getTemplateTypeName {
    return @"base";
}

#pragma mark - Layout Setup

- (void)setupLayout {
    // Override in subclasses
}

#pragma mark - JNativeAdLayout Protocol

- (GADNativeAdView *)getAdView {
    return self.nativeAdView;
}

- (UIView *)getQuitContainer {
    return self.quitLayoutContainer;
}

- (void)renderWithNativeAd:(GADNativeAd *)nativeAd {
    // Set headline
    self.headlineLabel.text = nativeAd.headline;
    
    // Set call to action
    [self.callToActionButton setTitle:nativeAd.callToAction forState:UIControlStateNormal];
    
    // Clear previous media content
    self.mediaView.mediaContent = nil;
    
    // Set icon
    if (nativeAd.icon.image) {
        self.iconImageView.image = nativeAd.icon.image;
        self.iconImageView.hidden = NO;
    } else {
        self.iconImageView.hidden = YES;
    }
    
    // Handle advertiser and store - show one or the other. Only the visible
    // label is registered as an asset view: the SDK's overlap validator
    // flags two registered asset views sharing the same frame even when one
    // is merely hidden, which is what causes "2 assets can not be placed on
    // top of another asset".
    if (nativeAd.advertiser && !nativeAd.store) {
        self.storeLabel.hidden = YES;
        self.nativeAdView.storeView = nil;
        self.advertiserLabel.text = nativeAd.advertiser;
        self.advertiserLabel.hidden = NO;
        self.nativeAdView.advertiserView = self.advertiserLabel;
    } else if (nativeAd.store && !nativeAd.advertiser) {
        self.advertiserLabel.hidden = YES;
        self.nativeAdView.advertiserView = nil;
        self.storeLabel.text = nativeAd.store;
        self.storeLabel.hidden = NO;
        self.nativeAdView.storeView = self.storeLabel;
    } else if (nativeAd.advertiser && nativeAd.store) {
        // Both present - default to advertiser
        self.storeLabel.hidden = YES;
        self.nativeAdView.storeView = nil;
        self.advertiserLabel.text = nativeAd.advertiser;
        self.advertiserLabel.hidden = NO;
        self.nativeAdView.advertiserView = self.advertiserLabel;
    } else {
        self.advertiserLabel.hidden = YES;
        self.nativeAdView.advertiserView = nil;
        self.storeLabel.hidden = YES;
        self.nativeAdView.storeView = nil;
    }

    // Handle star rating vs body text - same reasoning as above.
    if (nativeAd.starRating && nativeAd.starRating.floatValue > 0) {
        self.starRatingLabel.text = [self starStringFromRating:nativeAd.starRating];
        self.starRatingLabel.hidden = NO;
        self.nativeAdView.starRatingView = self.starRatingLabel;
        self.bodyLabel.hidden = YES;
        self.nativeAdView.bodyView = nil;
    } else {
        self.starRatingLabel.hidden = YES;
        self.nativeAdView.starRatingView = nil;
        self.bodyLabel.text = nativeAd.body;
        self.bodyLabel.hidden = NO;
        self.nativeAdView.bodyView = self.bodyLabel;
    }
    
    // Set media content - GADMediaView renders mediaContent.mainImage
    // automatically when there is no video, so no separate imageView is needed.
    self.mediaView.mediaContent = nativeAd.mediaContent;
    self.mediaView.hidden = NO;
    
    // ✅ CRITICAL: Assign nativeAd to nativeAdView to enable clicks and proper rendering
    // This is required by Google Mobile Ads SDK for:
    // 1. Making ad interactive (clickable)
    // 2. Proper size calculation and layout
    // 3. Impression and click tracking
    self.nativeAdView.nativeAd = nativeAd;
}

- (void)show {
    
    self.containerView.hidden = NO;
    [self.parentView bringSubviewToFront:self.containerView];
    
}

- (void)hide {
    self.containerView.hidden = YES;
}

- (void)destroy {
    self.mediaView.mediaContent = nil;
    [self.nativeAdView removeFromSuperview];
    [self.containerView removeFromSuperview];
    self.nativeAdView = nil;
    self.containerView = nil;
}

#pragma mark - Styling

- (void)applyStyles {
    // Get style values or defaults
    JNativeTemplateStyle *style = self.templateStyle ?: [[JNativeTemplateStyle alloc] initWithStyles:@{}];
    
    // Main background and border
    self.containerView.backgroundColor = [style styleForKey:JNativeTemplateStyleKeyMainBackgroundColor];
    
    CGFloat cornerRadius = [[style styleForKey:JNativeTemplateStyleKeyCornerRadius] floatValue];
    self.containerView.layer.cornerRadius = cornerRadius;
    self.containerView.clipsToBounds = YES;
    
    UIColor *borderColor = [style styleForKey:JNativeTemplateStyleKeyBorderColor];
    self.containerView.layer.borderColor = borderColor.CGColor;
    self.containerView.layer.borderWidth = [[style styleForKey:JNativeTemplateStyleKeyBorderWidth] floatValue];
    
    // Icon corner radius
    self.iconImageView.layer.cornerRadius = cornerRadius;
    self.iconImageView.clipsToBounds = YES;
    
    // Call to action button
    self.callToActionButton.layer.cornerRadius = cornerRadius;
    self.callToActionButton.clipsToBounds = YES;
    self.callToActionButton.backgroundColor = [style styleForKey:JNativeTemplateStyleKeyCallToActionBackgroundColor];
    [self.callToActionButton setTitleColor:[style styleForKey:JNativeTemplateStyleKeyCallToActionFontColor]
                                  forState:UIControlStateNormal];
    self.callToActionButton.titleLabel.font = [style styleForKey:JNativeTemplateStyleKeyCallToActionFont];
    
    // Headline (Primary text)
    self.headlineLabel.font = [style styleForKey:JNativeTemplateStyleKeyPrimaryFont];
    self.headlineLabel.textColor = [style styleForKey:JNativeTemplateStyleKeyPrimaryFontColor];
    self.headlineLabel.backgroundColor = [style styleForKey:JNativeTemplateStyleKeyPrimaryBackgroundColor];
    
    // Body (Secondary text)
    self.bodyLabel.font = [style styleForKey:JNativeTemplateStyleKeySecondaryFont];
    self.bodyLabel.textColor = [style styleForKey:JNativeTemplateStyleKeySecondaryFontColor];
    self.bodyLabel.backgroundColor = [style styleForKey:JNativeTemplateStyleKeySecondaryBackgroundColor];
    
    // Star rating (Secondary text style)
    self.starRatingLabel.font = [style styleForKey:JNativeTemplateStyleKeySecondaryFont];
    self.starRatingLabel.textColor = [UIColor colorWithRed:1.0 green:0.8 blue:0.0 alpha:1.0]; // Gold color for stars
    self.starRatingLabel.backgroundColor = [style styleForKey:JNativeTemplateStyleKeySecondaryBackgroundColor];
    
    // Advertiser (Tertiary text)
    self.advertiserLabel.font = [style styleForKey:JNativeTemplateStyleKeyTertiaryFont];
    self.advertiserLabel.textColor = [style styleForKey:JNativeTemplateStyleKeyTertiaryFontColor];
    self.advertiserLabel.backgroundColor = [style styleForKey:JNativeTemplateStyleKeyTertiaryBackgroundColor];
    
    // Store (Tertiary text)
    self.storeLabel.font = [style styleForKey:JNativeTemplateStyleKeyTertiaryFont];
    self.storeLabel.textColor = [style styleForKey:JNativeTemplateStyleKeyTertiaryFontColor];
    self.storeLabel.backgroundColor = [style styleForKey:JNativeTemplateStyleKeyTertiaryBackgroundColor];
    
    // Style Ad badge
    [self styleAdBadge];
}

- (void)styleAdBadge {
    if (!self.adBadgeLabel) return;
    
    self.adBadgeLabel.text = @"Ad";
    self.adBadgeLabel.font = [UIFont boldSystemFontOfSize:10];
    self.adBadgeLabel.textColor = [UIColor colorWithRed:0.0 green:0.48 blue:0.0 alpha:1.0];
    self.adBadgeLabel.textAlignment = NSTextAlignmentCenter;
    self.adBadgeLabel.layer.borderColor = self.adBadgeLabel.textColor.CGColor;
    self.adBadgeLabel.layer.borderWidth = 1.0;
    self.adBadgeLabel.layer.cornerRadius = 3.0;
    self.adBadgeLabel.layer.masksToBounds = YES;
}

#pragma mark - Helper Methods

- (NSString *)starStringFromRating:(NSNumber *)rating {
    if (!rating || rating.floatValue <= 0) {
        return @"";
    }
    
    NSMutableString *stars = [[NSMutableString alloc] init];
    int fullStars = (int)rating.integerValue;
    
    // Filled stars (★)
    for (int i = 0; i < fullStars && i < 5; i++) {
        [stars appendString:@"\u2605"];
    }
    
    // Empty stars (☆)
    for (int i = fullStars; i < 5; i++) {
        [stars appendString:@"\u2606"];
    }
    
    return [stars copy];
}

@end

