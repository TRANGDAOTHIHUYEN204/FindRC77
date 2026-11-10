# JNativeAds iOS Native Templates

This plugin implements native ad templates following the [Google AdMob Native Templates](https://developers.google.com/admob/ios/native/templates) guidelines.

## Template Types

### 1. Small Template (`JBannerLayout`)
**Similar to:** `GADTSmallTemplateView`

A compact horizontal layout suitable for banner-like placements.

**Layout Structure:**
```
[Ad Badge][Icon] | [Headline][Body/Store/Rating] | [CTA Button]
```

**Characteristics:**
- Compact size, similar to banner ads
- No media view displayed
- Horizontal layout
- Recommended size: 320×90 or similar

**Usage:**
```
templateName = "small" or "banner"
```

### 2. Medium Template - Portrait (`JPortraitLayout`)
**Similar to:** `GADTMediumTemplateView` (Portrait orientation)

A larger template with prominent media display at the top.

**Layout Structure:**
```
[Media View - Top 50%]
[Ad Badge] [Icon] [Headline/Body]
[Advertiser/Store]
[CTA Button - Full Width]
```

**Characteristics:**
- Larger size with media content
- Video/image media prominently displayed
- Full-width CTA button at bottom
- Recommended size: 320×320 or similar square/portrait aspect ratio

**Usage:**
```
templateName = "medium"
orientation = "portrait" (default)
```

### 3. Medium Template - Landscape (`JLandscapeLayout`)
**Similar to:** `GADTMediumTemplateView` (Landscape orientation)

A larger template with media on the left side.

**Layout Structure:**
```
[Media View - Left 45%] | [Ad Badge]
                         | [Icon] [Headline/Body]
                         | [Advertiser/Store]
                         | [CTA Button]
```

**Characteristics:**
- Horizontal layout
- Media on left side (45% width)
- Content on right side (55% width)
- Recommended size: 480×150 or similar landscape aspect ratio

**Usage:**
```
templateName = "medium"
orientation = "landscape"
```

## Style Customization

All templates support customization through `JNativeTemplateStyle`:

### Available Style Keys:

#### Call to Action Button
- `call_to_action_font` - UIFont
- `call_to_action_font_color` - UIColor
- `call_to_action_background_color` - UIColor

#### Primary Text (Headline)
- `primary_font` - UIFont
- `primary_font_color` - UIColor
- `primary_background_color` - UIColor

#### Secondary Text (Body/Rating)
- `secondary_font` - UIFont
- `secondary_font_color` - UIColor
- `secondary_background_color` - UIColor

#### Tertiary Text (Store/Advertiser)
- `tertiary_font` - UIFont
- `tertiary_font_color` - UIColor
- `tertiary_background_color` - UIColor

#### Container
- `main_background_color` - UIColor
- `corner_radius` - NSNumber
- `border_color` - UIColor
- `border_width` - NSNumber

## Ad Content Handling

### App Install Ads
For app install ads, the templates display:
- **Star Rating**: Shown as ★★★★☆ format
- **Store Name**: e.g., "App Store", "Google Play"
- **Icon**: App icon

### Content Ads
For content ads, the templates display:
- **Body Text**: Description text
- **Advertiser**: Brand/company name
- **Image/Video**: Media content

## References

- [Google AdMob Native Templates - iOS](https://developers.google.com/admob/ios/native/templates)
- [Google Native Templates GitHub Repository](https://github.com/googleads/googleads-mobile-ios-native-templates)

