#import "JInterClose.h"
#import "../JNativeConstants.h"
#import "JNativeOverlay.h"
#import "../Layouts/JPassthroughView.h"
#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>

// Mirrors Android's InterClose3 (quitType == "inter3"): full-width panel,
// 2-stage loading bar reveals a close button, decoy "Open Store" pill on
// the other side (non-clickable, taps fall through as a normal ad click).
// Sizing/timing constants live in JNativeConstants.h - change values there.

@interface JInterClose ()

@property(nonatomic, weak) JNativeOverlay *nativeOverlay;
@property(nonatomic, weak) UIView *quitContainer;
@property(nonatomic, strong) UIView *quitLayout;

@property(nonatomic, strong) UIView *loadingTrack1;
@property(nonatomic, strong) UIView *loadingTrack2;
@property(nonatomic, strong) UIView *loadingFill1;
@property(nonatomic, strong) UIView *loadingFill2;

@property(nonatomic, strong) UIView *boxClose;
@property(nonatomic, strong) UIView *haloView;
@property(nonatomic, strong) UIImageView *closeIconView;
@property(nonatomic, strong) UIButton *quitButton;

@property(nonatomic, strong) UIView *openStoreButton;
@property(nonatomic, assign) CGFloat openStoreWidth;

// 10s safety-net countdown (kJInterCloseFailsafeTimeout) - plain NSTimer,
// paused/resumed manually in onPause: since it isn't tied to quitLayout.layer.
@property(nonatomic, strong) NSTimer *failsafeTimer;
// Seconds left when paused (0 = not paused/not pending) - lets onPause:NO
// reschedule for the remaining time instead of the full timeout.
@property(nonatomic, assign) NSTimeInterval failsafePausedRemaining;

@property(nonatomic, assign) CGFloat buttonScale;

// Side for the NEXT onShow vs. the one currently on screen - keeps
// setButtonScale: resizing in place without re-randomizing sides.
@property(nonatomic, assign) BOOL nextOpenStoreOnRight;
@property(nonatomic, assign) BOOL currentOpenStoreOnRight;

// Mirrors onPause:'s state - lets stage 1's completion know whether it's
// safe to start stage 2 (see stage2PendingResume below).
@property(nonatomic, assign) BOOL isPaused;
// Same idea as stage2PendingResume, but for onShow: starting stage 1 while
// already paused would freeze it at 0% forever, so defer it here instead.
@property(nonatomic, assign) BOOL stage1PendingResume;
// Set when stage 1 finishes while paused - a brand-new animation isn't
// reliable on an already-paused layer, so stage 2 is deferred here until
// onPause:NO resumes it. Otherwise a pause on the stage1/2 boundary leaves
// the close button stuck forever.
@property(nonatomic, assign) BOOL stage2PendingResume;

@end

@implementation JInterClose

- (instancetype)initWithNativeOverlay:(JNativeOverlay *)overlay
                        quitContainer:(UIView *)container
                          buttonScale:(CGFloat)buttonScale {
  self = [super init];
  if (self) {
    _nativeOverlay = overlay;
    _quitContainer = container;
    // Ignore the caller-provided scale - fixed to match Android's tuning.
    _buttonScale = kJInterCloseFixedButtonScale;
    _nextOpenStoreOnRight = arc4random_uniform(2) == 0;
    [self setupQuitLayout];
  }
  return self;
}

#pragma mark - Setup

- (void)setupQuitLayout {
  CGFloat width = self.quitContainer.bounds.size.width;
  CGFloat height = self.quitContainer.bounds.size.height;

  // Pass-through: only box/store/button subviews absorb taps - everything
  // else must let clicks fall through to the ad content underneath.
  self.quitLayout = [[JPassthroughView alloc] initWithFrame:CGRectMake(0, 0, width, height)];

  // ---- Loading bar row: two segments, 70/30 width split like Android ----
  CGFloat barsUsableWidth = width - (kJInterCloseSideMargin * 2) - kJInterCloseBarGap;
  CGFloat bar1Width = MAX(barsUsableWidth * kJInterCloseBarWeightRatio, 0);
  CGFloat bar2Width = MAX(barsUsableWidth * (1.0 - kJInterCloseBarWeightRatio), 0);

  self.loadingTrack1 = [self makeTrackViewWithFrame:CGRectMake(kJInterCloseSideMargin, kJInterCloseBarTopMargin, bar1Width, kJInterCloseBarHeight)];
  self.loadingTrack2 = [self makeTrackViewWithFrame:CGRectMake(kJInterCloseSideMargin + bar1Width + kJInterCloseBarGap, kJInterCloseBarTopMargin, bar2Width, kJInterCloseBarHeight)];
  [self.quitLayout addSubview:self.loadingTrack1];
  [self.quitLayout addSubview:self.loadingTrack2];

  UIColor *fillColor = [UIColor colorWithRed:1.0 green:200.0 / 255.0 blue:0 alpha:1.0]; // #FFC800
  self.loadingFill1 = [[UIView alloc] initWithFrame:CGRectMake(0, 0, 0, kJInterCloseBarHeight)];
  self.loadingFill1.backgroundColor = fillColor;
  self.loadingFill1.layer.cornerRadius = kJInterCloseBarHeight / 2.0;
  [self.loadingTrack1 addSubview:self.loadingFill1];

  self.loadingFill2 = [[UIView alloc] initWithFrame:CGRectMake(0, 0, 0, kJInterCloseBarHeight)];
  self.loadingFill2.backgroundColor = fillColor;
  self.loadingFill2.layer.cornerRadius = kJInterCloseBarHeight / 2.0;
  [self.loadingTrack2 addSubview:self.loadingFill2];

  // ---- Close box: halo circle + ring/X glyph + invisible tap target ----
  self.boxClose = [[UIView alloc] initWithFrame:CGRectZero];
  self.boxClose.backgroundColor = [UIColor clearColor];
  [self.quitLayout addSubview:self.boxClose];

  self.haloView = [[UIView alloc] initWithFrame:CGRectZero];
  self.haloView.backgroundColor = [UIColor colorWithWhite:164.0 / 255.0 alpha:1.0]; // #A4A4A4
  [self.boxClose addSubview:self.haloView];

  self.closeIconView = [[UIImageView alloc] initWithFrame:CGRectZero];
  self.closeIconView.contentMode = UIViewContentModeScaleAspectFit;
  [self.boxClose addSubview:self.closeIconView];

  self.quitButton = [UIButton buttonWithType:UIButtonTypeCustom];
  [self.quitButton addTarget:self action:@selector(quitButtonTapped) forControlEvents:UIControlEventTouchUpInside];
  [self.boxClose addSubview:self.quitButton];

  // ---- Decoy "Open Store" pill - never clickable, purely visual ----
  // Temporarily hidden - flip back to NO to re-enable.
  self.openStoreButton = [self makeOpenStoreButton];
  self.openStoreButton.userInteractionEnabled = NO;
  self.openStoreButton.hidden = YES;
  [self.quitLayout addSubview:self.openStoreButton];

  [self.quitContainer addSubview:self.quitLayout];

  [self applyButtonScale];
  [self applySides:self.nextOpenStoreOnRight];
}

- (UIView *)makeTrackViewWithFrame:(CGRect)frame {
  UIView *track = [[UIView alloc] initWithFrame:frame];
  track.backgroundColor = [UIColor colorWithWhite:1.0 alpha:0.2]; // #33FFFFFF
  track.layer.cornerRadius = kJInterCloseBarHeight / 2.0;
  track.clipsToBounds = YES;
  return track;
}

- (UIView *)makeOpenStoreButton {
  UIFont *font = [UIFont boldSystemFontOfSize:12];
  NSString *text = @"Open Store";
  CGFloat textWidth = ceil([text sizeWithAttributes:@{NSFontAttributeName : font}].width);

  CGFloat paddingStart = 12;
  CGFloat paddingEnd = 14;
  CGFloat iconSize = 12;
  CGFloat gap = 6;
  self.openStoreWidth = paddingStart + iconSize + gap + textWidth + paddingEnd;

  UIView *button = [[UIView alloc] initWithFrame:CGRectMake(0, 0, self.openStoreWidth, kJInterCloseOpenStoreHeight)];
  button.backgroundColor = [UIColor colorWithRed:0x66 / 255.0 green:0x68 / 255.0 blue:0x6C / 255.0 alpha:1.0];
  button.layer.cornerRadius = kJInterCloseOpenStoreHeight / 2.0;

  UIImageView *playIcon = [[UIImageView alloc]
      initWithFrame:CGRectMake(paddingStart, (kJInterCloseOpenStoreHeight - iconSize) / 2.0, iconSize, iconSize)];
  playIcon.image = [[UIImage systemImageNamed:@"play.fill"] imageWithRenderingMode:UIImageRenderingModeAlwaysTemplate];
  playIcon.tintColor = [UIColor whiteColor];
  playIcon.contentMode = UIViewContentModeScaleAspectFit;
  [button addSubview:playIcon];

  UILabel *label = [[UILabel alloc]
      initWithFrame:CGRectMake(paddingStart + iconSize + gap, 0, textWidth, kJInterCloseOpenStoreHeight)];
  label.text = text;
  label.font = font;
  label.textColor = [UIColor whiteColor];
  [button addSubview:label];

  return button;
}

#pragma mark - Sizing / positioning

// quitButtonImage auto-scales with boxClose on Android; quitButton/halo are
// fixed-size there, so resize them explicitly here too.
- (void)applyButtonScale {
  CGFloat boxSize = kJInterCloseBoxCloseBaseSize * self.buttonScale;
  CGFloat buttonSize = kJInterCloseQuitButtonBaseSize * self.buttonScale;
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

// Positions boxClose/open-store on opposite sides (mirrors InterClose3's
// applySides). Must run after applyButtonScale so boxClose is sized.
- (void)applySides:(BOOL)openStoreOnRight {
  self.currentOpenStoreOnRight = openStoreOnRight;

  CGFloat width = self.quitLayout.bounds.size.width;
  CGFloat boxSize = self.boxClose.bounds.size.width;
  CGFloat rowY = kJInterCloseBarTopMargin + kJInterCloseBarHeight + kJInterCloseRowTopMargin;

  // Extra inset (on top of kJInterCloseSideMargin) only for the close
  // box/open-store pill, so they never sit flush against the screen edge
  // (notch/rounded corners) - the loading bars already have enough margin.
  CGFloat sideInset = kJInterCloseSideMargin + kJInterCloseHorizontalEdgePadding;

  CGFloat boxX = openStoreOnRight ? sideInset : (width - sideInset - boxSize);
  self.boxClose.frame = CGRectMake(boxX, rowY, boxSize, boxSize);

  CGFloat storeX = openStoreOnRight ? (width - sideInset - self.openStoreWidth) : sideInset;
  self.openStoreButton.frame = CGRectMake(storeX, rowY, self.openStoreWidth, kJInterCloseOpenStoreHeight);
}

// Draws the ring + X glyph (mirrors Android's ic_rc_close_2 vector).
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

#pragma mark - Animation

- (void)animateFillView:(UIView *)fillView
             trackWidth:(CGFloat)trackWidth
               duration:(NSTimeInterval)duration
             completion:(void (^)(void))completion {
  fillView.frame = CGRectMake(0, 0, 0, fillView.frame.size.height);
  [UIView animateWithDuration:duration
                        delay:0
                      options:UIViewAnimationOptionCurveLinear
                   animations:^{
                     fillView.frame = CGRectMake(0, 0, trackWidth, fillView.frame.size.height);
                   }
                   completion:^(BOOL finished) {
                     if (finished && completion) completion();
                   }];
}

#pragma mark - JQuitMethod

- (void)onShow {
  [self applyButtonScale];
  [self applySides:self.nextOpenStoreOnRight];
  self.nextOpenStoreOnRight = !self.nextOpenStoreOnRight;

  self.boxClose.hidden = YES;
  self.quitButton.enabled = NO;
  self.closeIconView.alpha = 0.2;
  self.haloView.alpha = 0.0;
  self.stage1PendingResume = NO;
  self.stage2PendingResume = NO;

  if (self.isPaused) {
    // Show() landed while already paused - defer stage 1 until onPause:NO
    // resumes, otherwise the whole sequence never starts.
    self.stage1PendingResume = YES;
    return;
  }
  [self startStage1Animation];
}

- (void)startStage1Animation {
  [self startFailsafeTimer];

  CGFloat track1Width = self.loadingTrack1.bounds.size.width;
  __weak typeof(self) weakSelf = self;
  [self animateFillView:self.loadingFill1
              trackWidth:track1Width
                duration:kJInterCloseLoadingStage1Duration
              completion:^{
                typeof(self) strongSelf = weakSelf;
                if (!strongSelf) return;
                strongSelf.boxClose.hidden = NO;
                strongSelf.haloView.alpha = 0.2;
                if (strongSelf.isPaused) {
                  // Pause landed on the stage1->stage2 boundary - defer
                  // stage 2 until onPause:NO resumes it.
                  strongSelf.stage2PendingResume = YES;
                  return;
                }
                [strongSelf startStage2Animation];
              }];
}

// Safety-net so the user is never stuck without a close button, whatever
// edge case the assumptions above miss. Started alongside stage 1; paused/
// resumed manually in onPause: since it's a plain timer, not layer-based.
- (void)startFailsafeTimer {
  [self.failsafeTimer invalidate];
  self.failsafePausedRemaining = 0;
  self.failsafeTimer = [self scheduleFailsafeTimerWithInterval:kJInterCloseFailsafeTimeout];
}

// NSRunLoopCommonModes so this still fires during UI tracking (e.g.
// scrolling), unlike scheduledTimerWithTimeInterval:'s default mode.
- (NSTimer *)scheduleFailsafeTimerWithInterval:(NSTimeInterval)interval {
  NSTimer *timer = [NSTimer timerWithTimeInterval:interval
                                            target:self
                                          selector:@selector(failsafeTimerFired)
                                          userInfo:nil
                                           repeats:NO];
  [[NSRunLoop mainRunLoop] addTimer:timer forMode:NSRunLoopCommonModes];
  return timer;
}

- (void)failsafeTimerFired {
  self.failsafeTimer = nil;
  [self forceUnlockIfNeeded];
}

- (void)forceUnlockIfNeeded {
  if (self.quitButton.enabled) return; // already unlocked normally

  [self.loadingFill1.layer removeAllAnimations];
  [self.loadingFill2.layer removeAllAnimations];
  self.loadingFill1.frame = CGRectMake(0, 0, self.loadingTrack1.bounds.size.width,
                                       self.loadingFill1.frame.size.height);
  self.loadingFill2.frame = CGRectMake(0, 0, self.loadingTrack2.bounds.size.width,
                                       self.loadingFill2.frame.size.height);
  self.stage1PendingResume = NO;
  self.stage2PendingResume = NO;
  [self applyUnlockedState];
}

- (void)startStage2Animation {
  CGFloat track2Width = self.loadingTrack2.bounds.size.width;
  __weak typeof(self) weakSelf = self;
  [self animateFillView:self.loadingFill2
              trackWidth:track2Width
                duration:kJInterCloseLoadingStage2Duration
              completion:^{
                [weakSelf applyUnlockedState];
              }];
}

- (void)applyUnlockedState {
  [self.failsafeTimer invalidate];
  self.failsafeTimer = nil;
  self.failsafePausedRemaining = 0;

  self.closeIconView.alpha = 1.0;
  self.haloView.alpha = 0.5;
  self.boxClose.hidden = NO;
  self.quitButton.enabled = YES;
}

- (void)quitButtonTapped {
  [self.nativeOverlay hide];
}

// Scale is fixed (kJInterCloseFixedButtonScale) - Unity's value is ignored.
- (void)setButtonScale:(CGFloat)scale {
}

// Pausing quitLayout.layer pauses every fill animation inside it (CA time
// is inherited by sublayers) - mirrors InterClose3's onPause behavior.
- (void)onPause:(BOOL)pause {
  self.isPaused = pause;
  CALayer *layer = self.quitLayout.layer;
  if (pause) {
    if (layer.speed != 0) {
      CFTimeInterval pausedTime = [layer convertTime:CACurrentMediaTime() fromLayer:nil];
      layer.speed = 0.0;
      layer.timeOffset = pausedTime;
    }
    // Plain NSTimer, not tied to quitLayout.layer - pause it manually by
    // remembering the seconds left and invalidating it.
    if (self.failsafeTimer) {
      self.failsafePausedRemaining = MAX(self.failsafeTimer.fireDate.timeIntervalSinceNow, 0);
      [self.failsafeTimer invalidate];
      self.failsafeTimer = nil;
    }
  } else {
    if (layer.speed == 0) {
      CFTimeInterval pausedTime = layer.timeOffset;
      layer.speed = 1.0;
      layer.timeOffset = 0.0;
      layer.beginTime = 0.0;
      CFTimeInterval timeSincePause = [layer convertTime:CACurrentMediaTime() fromLayer:nil] - pausedTime;
      layer.beginTime = timeSincePause;
    }
    if (self.failsafePausedRemaining > 0) {
      self.failsafeTimer = [self scheduleFailsafeTimerWithInterval:self.failsafePausedRemaining];
      self.failsafePausedRemaining = 0;
    }
    if (self.stage1PendingResume) {
      self.stage1PendingResume = NO;
      [self startStage1Animation];
    } else if (self.stage2PendingResume) {
      self.stage2PendingResume = NO;
      [self startStage2Animation];
    }
  }
}

- (void)destroy {
  [self.failsafeTimer invalidate];
  self.failsafeTimer = nil;
  [self.quitLayout removeFromSuperview];
  self.quitLayout = nil;
}

@end
