 // JNativeTemplateStyle.m
// Style configuration for native ad templates

#import "JNativeTemplateStyle.h"

#pragma mark - Style Key Constants

JNativeTemplateStyleKey const JNativeTemplateStyleKeyCallToActionFont = @"call_to_action_font";
JNativeTemplateStyleKey const JNativeTemplateStyleKeyCallToActionFontColor = @"call_to_action_font_color";
JNativeTemplateStyleKey const JNativeTemplateStyleKeyCallToActionBackgroundColor = @"call_to_action_background_color";

JNativeTemplateStyleKey const JNativeTemplateStyleKeyPrimaryFont = @"primary_font";
JNativeTemplateStyleKey const JNativeTemplateStyleKeyPrimaryFontColor = @"primary_font_color";
JNativeTemplateStyleKey const JNativeTemplateStyleKeyPrimaryBackgroundColor = @"primary_background_color";

JNativeTemplateStyleKey const JNativeTemplateStyleKeySecondaryFont = @"secondary_font";
JNativeTemplateStyleKey const JNativeTemplateStyleKeySecondaryFontColor = @"secondary_font_color";
JNativeTemplateStyleKey const JNativeTemplateStyleKeySecondaryBackgroundColor = @"secondary_background_color";

JNativeTemplateStyleKey const JNativeTemplateStyleKeyTertiaryFont = @"tertiary_font";
JNativeTemplateStyleKey const JNativeTemplateStyleKeyTertiaryFontColor = @"tertiary_font_color";
JNativeTemplateStyleKey const JNativeTemplateStyleKeyTertiaryBackgroundColor = @"tertiary_background_color";

JNativeTemplateStyleKey const JNativeTemplateStyleKeyMainBackgroundColor = @"main_background_color";
JNativeTemplateStyleKey const JNativeTemplateStyleKeyCornerRadius = @"corner_radius";
JNativeTemplateStyleKey const JNativeTemplateStyleKeyBorderColor = @"border_color";
JNativeTemplateStyleKey const JNativeTemplateStyleKeyBorderWidth = @"border_width";

@implementation JNativeTemplateStyle

- (instancetype)initWithStyles:(NSDictionary<JNativeTemplateStyleKey, id> *)styles {
    self = [super init];
    if (self) {
        _styles = [styles copy];
    }
    return self;
}

- (id)styleForKey:(JNativeTemplateStyleKey)key {
    return _styles[key] ?: [JNativeTemplateStyle defaultStyles][key];
}

+ (NSDictionary<JNativeTemplateStyleKey, id> *)defaultStyles {
    static NSDictionary *defaultStyles = nil;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        defaultStyles = @{
            JNativeTemplateStyleKeyCallToActionFont: [UIFont boldSystemFontOfSize:14],
            JNativeTemplateStyleKeyCallToActionFontColor: [UIColor whiteColor],
            JNativeTemplateStyleKeyCallToActionBackgroundColor: [self colorFromHexString:@"#007AFF"],
            
            JNativeTemplateStyleKeyPrimaryFont: [UIFont boldSystemFontOfSize:16],
            JNativeTemplateStyleKeyPrimaryFontColor: [UIColor whiteColor],
            JNativeTemplateStyleKeyPrimaryBackgroundColor: [UIColor clearColor],

            JNativeTemplateStyleKeySecondaryFont: [UIFont systemFontOfSize:13],
            JNativeTemplateStyleKeySecondaryFontColor: [UIColor whiteColor],
            JNativeTemplateStyleKeySecondaryBackgroundColor: [UIColor clearColor],

            JNativeTemplateStyleKeyTertiaryFont: [UIFont systemFontOfSize:12],
            JNativeTemplateStyleKeyTertiaryFontColor: [UIColor whiteColor],
            JNativeTemplateStyleKeyTertiaryBackgroundColor: [UIColor clearColor],

            JNativeTemplateStyleKeyMainBackgroundColor: [UIColor blackColor],
            JNativeTemplateStyleKeyCornerRadius: @(8),
            JNativeTemplateStyleKeyBorderColor: [self colorFromHexString:@"#E0E0E0"],
            JNativeTemplateStyleKeyBorderWidth: @(1)
        };
    });
    return defaultStyles;
}

+ (UIColor *)colorFromHexString:(NSString *)hexString {
    if (hexString == nil) {
        return nil;
    }
    
    NSRange range = [hexString rangeOfString:@"^#[0-9a-fA-F]{6}$" options:NSRegularExpressionSearch];
    if (range.location == NSNotFound) {
        // Try with 8 characters (with alpha)
        range = [hexString rangeOfString:@"^#[0-9a-fA-F]{8}$" options:NSRegularExpressionSearch];
        if (range.location == NSNotFound) {
            return nil;
        }
    }
    
    unsigned rgbValue = 0;
    NSScanner *scanner = [NSScanner scannerWithString:hexString];
    [scanner setScanLocation:1]; // Bypass '#' character
    [scanner scanHexInt:&rgbValue];
    
    if (hexString.length == 9) {
        // 8 hex digits with alpha
        return [UIColor colorWithRed:((rgbValue & 0xFF000000) >> 24) / 255.0f
                               green:((rgbValue & 0x00FF0000) >> 16) / 255.0f
                                blue:((rgbValue & 0x0000FF00) >> 8) / 255.0f
                               alpha:(rgbValue & 0x000000FF) / 255.0f];
    }
    
    return [UIColor colorWithRed:((rgbValue & 0xFF0000) >> 16) / 255.0f
                           green:((rgbValue & 0x00FF00) >> 8) / 255.0f
                            blue:(rgbValue & 0x0000FF) / 255.0f
                           alpha:1.0f];
}

@end

