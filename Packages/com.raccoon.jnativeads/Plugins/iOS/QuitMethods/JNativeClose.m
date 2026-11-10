#import "JNativeClose.h"
#import "JNativeOverlay.h"

// Base X button size (pt) at buttonScale == 1.0, independent of the
// container size cap - so a small scale like 0.2 still yields a properly
// tappable button instead of shrinking with the container too.
static const CGFloat kQuitButtonBaseSize = 76.0;

@interface JNativeClose ()

@property (nonatomic, weak) JNativeOverlay *nativeOverlay;
@property (nonatomic, weak) UIView *quitContainer;
@property (nonatomic, strong) UIButton *quitButton;
@property (nonatomic, assign) CGFloat buttonScale;

@end

@implementation JNativeClose

- (instancetype)initWithNativeOverlay:(JNativeOverlay *)overlay
                        quitContainer:(UIView *)container
                          buttonScale:(CGFloat)buttonScale {
    self = [super init];
    if (self) {
        _nativeOverlay = overlay;
        _quitContainer = container;
        _buttonScale = buttonScale > 0 ? buttonScale : 1.0;
        [self setupQuitButton];
    }
    return self;
}

- (void)setupQuitButton {
    // Get container size
    CGFloat containerWidth = self.quitContainer.bounds.size.width;
    CGFloat containerHeight = self.quitContainer.bounds.size.height;

    // Use smaller of width/height, but max 36pt for tappability, then apply scale
    // Dynamic sizing based on content
    CGFloat height = MIN(containerHeight, 36); // Max height 36pt
    CGFloat size = MIN(kQuitButtonBaseSize * self.buttonScale, height);

    // Center in container
    CGFloat x = (containerWidth - size) / 2;
    CGFloat y = (containerHeight - size) / 2;

    self.quitButton = [[UIButton alloc] initWithFrame:CGRectMake(x, y, size, size)];

    // X button style. Uses the "xmark" SF Symbol instead of the "✕" text
    // glyph - that character falls back to a font that ignores UIFont
    // weight, so setting boldSystemFont/.heavy on it had no visible effect.
    [self setXGlyphOnButton:self.quitButton size:size];
    self.quitButton.backgroundColor = [[UIColor colorWithWhite:0 alpha:0.7] colorWithAlphaComponent:0.8];
    self.quitButton.layer.cornerRadius = size / 2;
    self.quitButton.clipsToBounds = YES;

    // Add shadow for visibility
    self.quitButton.layer.shadowColor = [UIColor blackColor].CGColor;
    self.quitButton.layer.shadowOffset = CGSizeMake(0, 2);
    self.quitButton.layer.shadowRadius = 4;
    self.quitButton.layer.shadowOpacity = 0.3;
    self.quitButton.layer.masksToBounds = NO;

    [self.quitButton addTarget:self action:@selector(quitButtonTapped) forControlEvents:UIControlEventTouchUpInside];
    [self.quitContainer addSubview:self.quitButton];

}

- (void)setXGlyphOnButton:(UIButton *)button size:(CGFloat)size {
    UIImageSymbolConfiguration *config = [UIImageSymbolConfiguration configurationWithPointSize:MAX(size * 0.5, 8) weight:UIImageSymbolWeightBlack scale:UIImageSymbolScaleLarge];
    UIImage *xImage = [[UIImage systemImageNamed:@"xmark" withConfiguration:config] imageWithRenderingMode:UIImageRenderingModeAlwaysTemplate];
    [button setImage:xImage forState:UIControlStateNormal];
    button.tintColor = [UIColor whiteColor];
}

- (void)onShow {

    // Disable 2 giây đầu
    self.quitButton.enabled = NO;
    self.quitButton.alpha = 0.5;

    dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(2.0 * NSEC_PER_SEC)), dispatch_get_main_queue(), ^{
        self.quitButton.enabled = YES;
        self.quitButton.alpha = 1.0;
    });
}

- (void)quitButtonTapped {
    [self.nativeOverlay hide];
}

- (void)setButtonScale:(CGFloat)scale {
    self.buttonScale = scale > 0 ? scale : 1.0;

    if (!self.quitButton) return;

    CGFloat containerWidth = self.quitContainer.bounds.size.width;
    CGFloat containerHeight = self.quitContainer.bounds.size.height;
    CGFloat height = MIN(containerHeight, 36); // Max height 36pt
    CGFloat size = MIN(kQuitButtonBaseSize * self.buttonScale, height);
    CGFloat x = (containerWidth - size) / 2;
    CGFloat y = (containerHeight - size) / 2;

    self.quitButton.frame = CGRectMake(x, y, size, size);
    self.quitButton.layer.cornerRadius = size / 2;
    [self setXGlyphOnButton:self.quitButton size:size];

}

- (void)destroy {
    [self.quitButton removeFromSuperview];
    self.quitButton = nil;
}

@end
