#import "JInterClose2.h"
#import "../JNativeConstants.h"
#import "../Layouts/JPassthroughView.h"
#import "JNativeOverlay.h"

// Story-style segmented bar (one segment per ad in a multi-ad sequence) plus
// a close box that fades in kJInterClose2RevealBeforeEnd before the countdown
// ends. Wall-clock timers on purpose - Android's InterClose2 doesn't pause
// either, and an advance on resume restarts everything via onShow anyway.

@interface JInterClose2 ()

@property(nonatomic, weak) JNativeOverlay *nativeOverlay;
@property(nonatomic, weak) UIView *quitContainer;
@property(nonatomic, strong) UIView *quitLayout;

@property(nonatomic, strong) NSArray<UIView *> *loadingTracks;
@property(nonatomic, strong) NSArray<UIView *> *loadingFills;

@property(nonatomic, strong) UIView *boxClose;
@property(nonatomic, strong) UIView *haloView;
@property(nonatomic, strong) UIImageView *closeIconView;
@property(nonatomic, strong) UIButton *quitButton;
@property(nonatomic, assign) BOOL boxOnRight;

@property(nonatomic, assign) NSInteger totalAdsInSequence;
@property(nonatomic, assign) NSInteger currentAdIndexInSequence;

@property(nonatomic, strong) NSTimer *revealTimer;
@property(nonatomic, strong) NSTimer *unlockTimer;
// Non-nil only while the current segment is animating - its progress is
// derived from this, so a re-split can resume the fill where it was.
@property(nonatomic, strong) NSDate *segmentStartDate;
@property(nonatomic, assign) CGFloat frozenProgress;

@end

@implementation JInterClose2

- (instancetype)initWithNativeOverlay:(JNativeOverlay *)overlay
                        quitContainer:(UIView *)container {
  self = [super init];
  if (self) {
    _nativeOverlay = overlay;
    _quitContainer = container;
    _totalAdsInSequence = 1;
    _currentAdIndexInSequence = 1;
    _boxOnRight = arc4random_uniform(2) == 0;
    [self setupQuitLayout];
  }
  return self;
}

#pragma mark - Setup

- (void)setupQuitLayout {
  CGRect bounds = self.quitContainer.bounds;
  self.quitLayout = [[JPassthroughView alloc] initWithFrame:bounds];

  UIColor *trackColor = [UIColor colorWithWhite:1.0 alpha:0.2]; // #33FFFFFF
  UIColor *fillColor = [UIColor colorWithRed:1.0 green:200.0 / 255.0 blue:0 alpha:1.0]; // #FFC800
  NSMutableArray *tracks = [NSMutableArray array];
  NSMutableArray *fills = [NSMutableArray array];
  for (NSInteger i = 0; i < kJInterClose2MaxSegments; i++) {
    UIView *track = [[UIView alloc] initWithFrame:CGRectZero];
    track.backgroundColor = trackColor;
    track.layer.cornerRadius = kJInterCloseBarHeight / 2.0;
    track.clipsToBounds = YES;
    track.userInteractionEnabled = NO;
    [self.quitLayout addSubview:track];

    UIView *fill = [[UIView alloc] initWithFrame:CGRectMake(0, 0, 0, kJInterCloseBarHeight)];
    fill.backgroundColor = fillColor;
    fill.layer.cornerRadius = kJInterCloseBarHeight / 2.0;
    [track addSubview:fill];

    [tracks addObject:track];
    [fills addObject:fill];
  }
  self.loadingTracks = tracks;
  self.loadingFills = fills;

  self.boxClose = [[UIView alloc] initWithFrame:CGRectZero];
  self.boxClose.backgroundColor = [UIColor clearColor];
  self.boxClose.hidden = YES;
  [self.quitLayout addSubview:self.boxClose];

  self.haloView = [[UIView alloc] initWithFrame:CGRectZero];
  self.haloView.backgroundColor = [UIColor colorWithWhite:164.0 / 255.0 alpha:1.0]; // #A4A4A4
  self.haloView.userInteractionEnabled = NO;
  [self.boxClose addSubview:self.haloView];

  self.closeIconView = [[UIImageView alloc] initWithFrame:CGRectZero];
  self.closeIconView.contentMode = UIViewContentModeScaleAspectFit;
  [self.boxClose addSubview:self.closeIconView];

  self.quitButton = [UIButton buttonWithType:UIButtonTypeCustom];
  self.quitButton.enabled = NO;
  [self.quitButton addTarget:self
                      action:@selector(quitButtonTapped)
            forControlEvents:UIControlEventTouchUpInside];
  [self.boxClose addSubview:self.quitButton];

  [self.quitContainer addSubview:self.quitLayout];

  [self applyButtonScale];
  [self applySides:self.boxOnRight];
  [self renderSegmentsWithProgress:0 animate:NO];
}

#pragma mark - Sizing / positioning

- (void)applyButtonScale {
  CGFloat scale = kJInterCloseFixedButtonScale;
  CGFloat boxSize = kJInterCloseBoxCloseBaseSize * scale;
  CGFloat buttonSize = kJInterCloseQuitButtonBaseSize * scale;
  CGFloat haloSize = boxSize * kJInterCloseHaloScale;

  CGRect boxFrame = self.boxClose.frame;
  self.boxClose.frame = CGRectMake(boxFrame.origin.x, boxFrame.origin.y, boxSize, boxSize);

  self.haloView.bounds = CGRectMake(0, 0, haloSize, haloSize);
  self.haloView.center = CGPointMake(boxSize / 2.0, boxSize / 2.0);
  self.haloView.layer.cornerRadius = haloSize / 2.0;

  self.closeIconView.frame = CGRectMake(0, 0, boxSize, boxSize);
  self.closeIconView.image = [self closeGlyphImageWithSize:boxSize];

  self.quitButton.bounds = CGRectMake(0, 0, buttonSize, buttonSize);
  self.quitButton.center = CGPointMake(boxSize / 2.0, boxSize / 2.0);
}

- (void)applySides:(BOOL)boxOnRight {
  CGFloat width = self.quitLayout.bounds.size.width;
  CGFloat boxSize = self.boxClose.bounds.size.width;
  CGFloat rowY = kJInterCloseBarTopMargin + kJInterCloseBarHeight + kJInterCloseRowTopMargin;
  CGFloat sideInset = kJInterCloseSideMargin + kJInterCloseHorizontalEdgePadding;
  CGFloat boxX = boxOnRight ? (width - sideInset - boxSize) : sideInset;
  self.boxClose.frame = CGRectMake(boxX, rowY, boxSize, boxSize);
}

// With fewer ads than tracks, unused tracks are hidden and the visible ones
// split the full width evenly (LinearLayout weights on Android).
- (void)layoutTracks {
  NSInteger visible = MIN(MAX(self.totalAdsInSequence, 1), kJInterClose2MaxSegments);
  CGFloat width = self.quitLayout.bounds.size.width;
  CGFloat usable = width - (kJInterCloseSideMargin * 2) - kJInterCloseBarGap * (visible - 1);
  CGFloat trackWidth = MAX(usable / visible, 0);
  for (NSInteger i = 0; i < kJInterClose2MaxSegments; i++) {
    UIView *track = self.loadingTracks[i];
    track.hidden = i >= visible;
    track.frame = CGRectMake(kJInterCloseSideMargin + i * (trackWidth + kJInterCloseBarGap),
                             kJInterCloseBarTopMargin, trackWidth, kJInterCloseBarHeight);
  }
}

// Draws the ring + X glyph (same as JInterClose / Android's ic_rc_close_2).
- (UIImage *)closeGlyphImageWithSize:(CGFloat)size {
  if (size <= 0) return nil;

  UIGraphicsImageRendererFormat *format = [UIGraphicsImageRendererFormat preferredFormat];
  format.opaque = NO;
  UIGraphicsImageRenderer *renderer =
      [[UIGraphicsImageRenderer alloc] initWithSize:CGSizeMake(size, size) format:format];

  return [renderer imageWithActions:^(UIGraphicsImageRendererContext *_Nonnull rendererContext) {
    CGContextRef ctx = rendererContext.CGContext;
    CGContextSetStrokeColorWithColor(ctx, [UIColor whiteColor].CGColor);

    CGFloat ringInset = size * 0.05;
    CGContextSetLineWidth(ctx, MAX(size * 0.05, 1.0));
    CGContextStrokeEllipseInRect(ctx, CGRectInset(CGRectMake(0, 0, size, size), ringInset, ringInset));

    CGFloat xInset = size * 0.32;
    CGContextSetLineWidth(ctx, MAX(size * 0.075, 1.5));
    CGContextSetLineCap(ctx, kCGLineCapRound);
    CGContextMoveToPoint(ctx, xInset, xInset);
    CGContextAddLineToPoint(ctx, size - xInset, size - xInset);
    CGContextMoveToPoint(ctx, size - xInset, xInset);
    CGContextAddLineToPoint(ctx, xInset, size - xInset);
    CGContextStrokePath(ctx);
  }];
}

#pragma mark - Segments

- (CGFloat)currentSegmentProgress {
  if (!self.segmentStartDate) return self.frozenProgress;
  NSTimeInterval elapsed = -[self.segmentStartDate timeIntervalSinceNow];
  return MIN(MAX(elapsed / kJInterClose2CountdownDuration, 0), 1);
}

// Segments before the current one stay full (ads already got past), later
// ones empty; the current one starts at `progress` and, if animate, fills
// over the remaining share of the countdown.
- (void)renderSegmentsWithProgress:(CGFloat)progress animate:(BOOL)animate {
  [self layoutTracks];
  NSInteger currentIndex = self.currentAdIndexInSequence - 1;
  for (NSInteger i = 0; i < kJInterClose2MaxSegments; i++) {
    UIView *fill = self.loadingFills[i];
    [fill.layer removeAllAnimations];
    CGFloat trackWidth = self.loadingTracks[i].bounds.size.width;
    CGFloat fillWidth = 0;
    if (i < currentIndex) {
      fillWidth = trackWidth;
    } else if (i == currentIndex) {
      fillWidth = trackWidth * progress;
    }
    fill.frame = CGRectMake(0, 0, fillWidth, kJInterCloseBarHeight);
  }

  if (!animate || progress >= 1 || currentIndex < 0 || currentIndex >= kJInterClose2MaxSegments) {
    return;
  }
  UIView *current = self.loadingFills[currentIndex];
  CGFloat trackWidth = self.loadingTracks[currentIndex].bounds.size.width;
  [UIView animateWithDuration:(1 - progress) * kJInterClose2CountdownDuration
                        delay:0
                      options:UIViewAnimationOptionCurveLinear
                   animations:^{
                     current.frame = CGRectMake(0, 0, trackWidth, kJInterCloseBarHeight);
                   }
                   completion:nil];
}

- (void)freezeCurrentSegment {
  self.frozenProgress = [self currentSegmentProgress];
  self.segmentStartDate = nil;
  [self renderSegmentsWithProgress:self.frozenProgress animate:NO];
}

#pragma mark - Sequence

- (void)setTotalAdsInSequence:(NSInteger)count {
  _totalAdsInSequence = MAX(count, 1);
}

- (void)setActiveSlot:(NSInteger)index {
  self.currentAdIndexInSequence = MAX(index, 1);
}

- (void)refreshSequenceLength:(NSInteger)count {
  count = MAX(count, 1);
  if (count == self.totalAdsInSequence) return;
  self.totalAdsInSequence = count;
  [self renderSegmentsWithProgress:[self currentSegmentProgress]
                           animate:self.segmentStartDate != nil];
}

#pragma mark - Timers

// Block-based (doesn't retain self) + common modes (fires during UI tracking).
- (NSTimer *)scheduleTimerAfter:(NSTimeInterval)interval block:(void (^)(JInterClose2 *))block {
  __weak typeof(self) weakSelf = self;
  NSTimer *timer = [NSTimer timerWithTimeInterval:interval
                                          repeats:NO
                                            block:^(NSTimer *_Nonnull t) {
                                              JInterClose2 *strongSelf = weakSelf;
                                              if (strongSelf) block(strongSelf);
                                            }];
  [[NSRunLoop mainRunLoop] addTimer:timer forMode:NSRunLoopCommonModes];
  return timer;
}

- (void)cancelTimers {
  [self.revealTimer invalidate];
  self.revealTimer = nil;
  [self.unlockTimer invalidate];
  self.unlockTimer = nil;
}

- (void)revealLockedBox {
  self.revealTimer = nil;
  self.closeIconView.alpha = 0.2;
  self.haloView.alpha = 0.2;
  self.boxClose.hidden = NO;
}

- (void)applyUnlockedState {
  [self cancelTimers];
  self.closeIconView.alpha = 1.0;
  self.haloView.alpha = 0.5;
  self.boxClose.hidden = NO;
  self.quitButton.enabled = YES;
}

#pragma mark - JQuitMethod

- (void)onShow {
  [self cancelTimers];
  [self applyButtonScale];
  // Side is picked on the first ad only - ad2/ad3 keep ad1's position.
  if (self.currentAdIndexInSequence == 1) {
    [self applySides:self.boxOnRight];
    self.boxOnRight = !self.boxOnRight;
  }

  self.quitButton.enabled = NO;
  self.closeIconView.alpha = 0.0;
  self.haloView.alpha = 0.0;
  self.boxClose.hidden = YES;

  self.frozenProgress = 0;
  self.segmentStartDate = [NSDate date];
  [self renderSegmentsWithProgress:0 animate:YES];

  self.revealTimer =
      [self scheduleTimerAfter:kJInterClose2CountdownDuration - kJInterClose2RevealBeforeEnd
                         block:^(JInterClose2 *s) { [s revealLockedBox]; }];
  self.unlockTimer = [self scheduleTimerAfter:kJInterClose2CountdownDuration
                                        block:^(JInterClose2 *s) { [s applyUnlockedState]; }];
}

- (void)showUnlocked {
  [self applyButtonScale];
  [self applyUnlockedState];
}

// Hides X right away (ad click or X itself) instead of leaving it visible
// while the overlay waits to advance. No-op outside a multi-ad sequence.
- (void)hideQuitButtonImmediately {
  if (self.totalAdsInSequence <= 1) return;
  [self cancelTimers];
  [self freezeCurrentSegment];
  self.quitButton.enabled = NO;
  self.closeIconView.alpha = 0.0;
  self.haloView.alpha = 0.0;
  self.boxClose.hidden = YES;
}

- (void)quitButtonTapped {
  [self hideQuitButtonImmediately];
  [self.nativeOverlay onQuitRequested];
}

- (void)onAdClicked {
  [self hideQuitButtonImmediately];
}

// Scale is fixed (kJInterCloseFixedButtonScale) - Unity's value is ignored.
- (void)setButtonScale:(CGFloat)scale {
}

- (void)destroy {
  [self cancelTimers];
  [self.quitLayout removeFromSuperview];
  self.quitLayout = nil;
}

@end
