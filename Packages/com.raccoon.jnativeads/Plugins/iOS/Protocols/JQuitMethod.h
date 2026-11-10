#ifndef JQuitMethod_h
#define JQuitMethod_h

#import <Foundation/Foundation.h>
#import <CoreGraphics/CoreGraphics.h>

@protocol JQuitMethod <NSObject>

- (void)onShow;
- (void)destroy;

@optional
- (void)setButtonScale:(CGFloat)scale;
- (void)onPause:(BOOL)pause;
- (void)onAdClicked;
- (void)showUnlocked;

@end

#endif