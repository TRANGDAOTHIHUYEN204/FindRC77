#ifndef JNativeConstants_h
#define JNativeConstants_h

#import <CoreGraphics/CoreGraphics.h>
#import <Foundation/Foundation.h>

// Single source of truth for JInterClose's (InterClose3-style) sizing.
// Shared with JPortraitLayout/JLandscapeLayout, which need the same box size
// to reserve room for the panel - change values here only, nothing else.

// Fixed size tuning - matches the Android values (buttonScale 0.38,
// scaleCover/quitButtonImage2Scale 1.8) hardcoded rather than taking
// whatever scale the caller passes in.
static const CGFloat kJInterCloseFixedButtonScale = 0.38;
// halo circle diameter relative to boxClose
static const CGFloat kJInterCloseHaloScale = 1.8;

// Base sizes (pt) at buttonScale == 1.0, matching InterClose3's dp constants.
static const CGFloat kJInterCloseBoxCloseBaseSize = 50.0;
static const CGFloat kJInterCloseQuitButtonBaseSize = 30.0;

static const NSTimeInterval kJInterCloseLoadingStage1Duration = 3.0;
static const NSTimeInterval kJInterCloseLoadingStage2Duration = 2.0;

static const CGFloat kJInterCloseBarHeight = 4.0;
static const CGFloat kJInterCloseBarTopMargin = 2.0;
static const CGFloat kJInterCloseBarGap = 4.0;
// first segment gets 70% of the bar row
static const CGFloat kJInterCloseBarWeightRatio = 0.7;

static const CGFloat kJInterCloseRowTopMargin = 6.0;
static const CGFloat kJInterCloseSideMargin = 10.0;
static const CGFloat kJInterCloseOpenStoreHeight = 30.0;

// Extra horizontal inset applied to the whole panel (on top of
// kJInterCloseSideMargin above, which only spaces the bars/box/store from
// each other and mirrors Android 1:1) so the close box/open-store pill never
// sit flush against the physical screen edge (rounded corners/notch can clip
// or visually cover them there).
static const CGFloat kJInterCloseHorizontalEdgePadding = 12.0;

// Safety-net: if the 2-stage loading sequence (normally
// kJInterCloseLoadingStage1Duration + kJInterCloseLoadingStage2Duration ==
// 5s) hasn't finished/unlocked the close button by this long after onShow,
// force it to the finished state so the user is never permanently stuck.
// Comfortably above the normal total so it only ever fires as a fallback.
static const NSTimeInterval kJInterCloseFailsafeTimeout = 10.0;

// Temporarily disabled - auto-refresh while showing is off. Flip this back
// to YES to bring it back.
static const BOOL kJNativeAutoRefreshEnabled = NO;

// Delay before reloading after a click, on its own timer separate from the
// auto-refresh one above - so toggling kJNativeAutoRefreshEnabled never
// affects click-triggered reloads.
static const NSTimeInterval kJNativeClickReloadDelay = 0.25;

// ---- JInterClose2 (Android InterClose2, quitType "inter2") ----
// Countdown/segment fill length, and how long before its end the close box
// fades in (still locked) - matches InterClose2's 5000ms / 2000ms.
static const NSTimeInterval kJInterClose2CountdownDuration = 5.0;
static const NSTimeInterval kJInterClose2RevealBeforeEnd = 2.0;
static const NSInteger kJInterClose2MaxSegments = 3;

// Multi-ad ("id1_id2_id3" + inter2): if a click never opens anything (no
// app resign, no in-app screen), advance anyway after this long.
static const NSTimeInterval kJNativeMultiAdClickAdvanceFallback = 0.25;

#endif
