// JBaseNativeLayout.h
// Base class for native ad layouts with common functionality

#import <UIKit/UIKit.h>
#import <GoogleMobileAds/GoogleMobileAds.h>
#import "JNativeAdLayout.h"
#import "JNativeTemplateStyle.h"

@class JNativeOverlay;

NS_ASSUME_NONNULL_BEGIN

@interface JBaseNativeLayout : NSObject <JNativeAdLayout>

#pragma mark - Properties
@property (nonatomic, weak) JNativeOverlay *nativeOverlay;
@property (nonatomic, weak) UIView *parentView;
@property (nonatomic, strong, nullable) JNativeTemplateStyle *templateStyle;

#pragma mark - Views
@property (nonatomic, strong) UIView *containerView;
@property (nonatomic, strong) GADNativeAdView *nativeAdView;
@property (nonatomic, strong) UILabel *headlineLabel;
@property (nonatomic, strong) UILabel *bodyLabel;
@property (nonatomic, strong) UILabel *advertiserLabel;
@property (nonatomic, strong) UILabel *storeLabel;
@property (nonatomic, strong) UILabel *starRatingLabel;
@property (nonatomic, strong) UILabel *adBadgeLabel;
@property (nonatomic, strong) UIButton *callToActionButton;
@property (nonatomic, strong) UIImageView *iconImageView;
@property (nonatomic, strong) GADMediaView *mediaView;
@property (nonatomic, strong) UIView *quitLayoutContainer;

#pragma mark - Initialization
- (instancetype)initWithNativeOverlay:(JNativeOverlay *)overlay
                           parentView:(UIView *)parentView;

- (instancetype)initWithNativeOverlay:(JNativeOverlay *)overlay
                           parentView:(UIView *)parentView
                                style:(nullable JNativeTemplateStyle *)style;

#pragma mark - Template Configuration
/// Override in subclass to return the template type name
- (NSString *)getTemplateTypeName;

#pragma mark - Layout Setup (Override in subclasses)
- (void)setupLayout;

#pragma mark - Styling
/// Apply styles to all views
- (void)applyStyles;

/// Style the Ad badge
- (void)styleAdBadge;

#pragma mark - Helper Methods
/// Creates a star rating string from rating value
- (NSString *)starStringFromRating:(NSNumber *)rating;

@end

NS_ASSUME_NONNULL_END

