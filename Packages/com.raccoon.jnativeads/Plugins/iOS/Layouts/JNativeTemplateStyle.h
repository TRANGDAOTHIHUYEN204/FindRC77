// JNativeTemplateStyle.h
// Style configuration for native ad templates
// Inspired by Google Native Templates

#import <UIKit/UIKit.h>

/// Constants used to style your template.
typedef NSString* JNativeTemplateStyleKey NS_STRING_ENUM;

#pragma mark - Call To Action
/// Call to action font. Expects a UIFont.
extern JNativeTemplateStyleKey const JNativeTemplateStyleKeyCallToActionFont;
/// Call to action font color. Expects a UIColor.
extern JNativeTemplateStyleKey const JNativeTemplateStyleKeyCallToActionFontColor;
/// Call to action background color. Expects a UIColor.
extern JNativeTemplateStyleKey const JNativeTemplateStyleKeyCallToActionBackgroundColor;

#pragma mark - Primary Text (Headline)
/// Primary text font. Expects a UIFont.
extern JNativeTemplateStyleKey const JNativeTemplateStyleKeyPrimaryFont;
/// Primary text font color. Expects a UIColor.
extern JNativeTemplateStyleKey const JNativeTemplateStyleKeyPrimaryFontColor;
/// Primary text background color. Expects a UIColor.
extern JNativeTemplateStyleKey const JNativeTemplateStyleKeyPrimaryBackgroundColor;

#pragma mark - Secondary Text (Body/Rating)
/// Secondary text font. Expects a UIFont.
extern JNativeTemplateStyleKey const JNativeTemplateStyleKeySecondaryFont;
/// Secondary text font color. Expects a UIColor.
extern JNativeTemplateStyleKey const JNativeTemplateStyleKeySecondaryFontColor;
/// Secondary text background color. Expects a UIColor.
extern JNativeTemplateStyleKey const JNativeTemplateStyleKeySecondaryBackgroundColor;

#pragma mark - Tertiary Text (Store/Advertiser)
/// Tertiary text font. Expects a UIFont.
extern JNativeTemplateStyleKey const JNativeTemplateStyleKeyTertiaryFont;
/// Tertiary text font color. Expects a UIColor.
extern JNativeTemplateStyleKey const JNativeTemplateStyleKeyTertiaryFontColor;
/// Tertiary text background color. Expects a UIColor.
extern JNativeTemplateStyleKey const JNativeTemplateStyleKeyTertiaryBackgroundColor;

#pragma mark - Additional Style Options
/// The background color for the bulk of the ad. Expects a UIColor.
extern JNativeTemplateStyleKey const JNativeTemplateStyleKeyMainBackgroundColor;
/// The corner rounding radius for the icon view and call to action. Expects an NSNumber.
extern JNativeTemplateStyleKey const JNativeTemplateStyleKeyCornerRadius;
/// Border color. Expects a UIColor.
extern JNativeTemplateStyleKey const JNativeTemplateStyleKeyBorderColor;
/// Border width. Expects an NSNumber.
extern JNativeTemplateStyleKey const JNativeTemplateStyleKeyBorderWidth;

@interface JNativeTemplateStyle : NSObject

@property (nonatomic, copy, readonly) NSDictionary<JNativeTemplateStyleKey, id> *styles;

- (instancetype)initWithStyles:(NSDictionary<JNativeTemplateStyleKey, id> *)styles;

/// Get style value for key
- (id)styleForKey:(JNativeTemplateStyleKey)key;

/// Creates an opaque UIColor object from a hex string (e.g., "#FF5733").
+ (UIColor *)colorFromHexString:(NSString *)hexString;

/// Get default styles
+ (NSDictionary<JNativeTemplateStyleKey, id> *)defaultStyles;

@end

