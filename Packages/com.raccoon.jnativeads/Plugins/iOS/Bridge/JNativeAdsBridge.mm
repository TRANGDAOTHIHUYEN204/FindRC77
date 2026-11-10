#import "../JNativeOverlay.h"
#import "../JNativeOverlayCallback.h"
#import <Foundation/Foundation.h>
#import <objc/runtime.h>

#pragma mark - Unity Callback Handler

@interface JUnityNativeOverlayCallback : NSObject <JNativeOverlayCallback>

@property(nonatomic, copy) NSString *instanceId;
@property(nonatomic, copy) NSString *gameObjectName;

@end

@implementation JUnityNativeOverlayCallback

- (instancetype)initWithInstanceId:(NSString *)instanceId
                    gameObjectName:(NSString *)gameObjectName {
  self = [super init];
  if (self) {
    _instanceId = instanceId;
    _gameObjectName = gameObjectName;
  }
  return self;
}

- (void)sendMessageToUnity:(NSString *)method message:(NSString *)message {
  // Format: "instanceId|data"
  NSString *fullMessage =
      [NSString stringWithFormat:@"%@|%@", self.instanceId, message ?: @""];

  const char *gameObject = [self.gameObjectName UTF8String];
  const char *methodName = [method UTF8String];
  const char *msg = [fullMessage UTF8String];

  extern void UnitySendMessage(const char *, const char *, const char *);
  UnitySendMessage(gameObject, methodName, msg);
}

- (void)onLoadSucceed {
  [self sendMessageToUnity:@"OnLoadSucceed" message:nil];
}

- (void)onLoadFailedWithError:(NSError *)error {
  NSString *errorMessage = error.localizedDescription ?: @"Unknown error";
  [self sendMessageToUnity:@"OnLoadFailed" message:errorMessage];
}

- (void)onClosed {
  [self sendMessageToUnity:@"OnClosed" message:nil];
}

- (void)onClicked {
  [self sendMessageToUnity:@"OnClicked" message:nil];
}

- (void)onImpression {
  [self sendMessageToUnity:@"OnImpression" message:nil];
}

- (void)onAdPaidWithValue:(GADAdValue *)value {
  // Gửi ad value dưới dạng JSON
  NSDecimalNumber *micros =
      [value.value decimalNumberByMultiplyingByPowerOf10:6];
  NSDictionary *adValueDict = @{
    @"currencyCode" : value.currencyCode ?: @"",
    @"valueMicros" : @(micros.longLongValue), // send as number, not string
    @"precision" : @(value.precision)
  };

  NSError *jsonError;
  NSData *jsonData = [NSJSONSerialization dataWithJSONObject:adValueDict
                                                     options:0
                                                       error:&jsonError];
  NSString *jsonString =
      jsonData ? [[NSString alloc] initWithData:jsonData
                                       encoding:NSUTF8StringEncoding]
               : @"{}";

  [self sendMessageToUnity:@"OnAdPaid" message:jsonString];
}

@end

#pragma mark - C Interface for Unity

static NSMutableDictionary<NSString *, JNativeOverlay *> *overlayInstances;
static NSMutableDictionary<NSString *, JUnityNativeOverlayCallback *>
    *callbackInstances;

extern "C" {

void JNativeAds_Initialize() {
  if (!overlayInstances) {
    overlayInstances = [NSMutableDictionary dictionary];
  }
  if (!callbackInstances) {
    callbackInstances = [NSMutableDictionary dictionary];
  }
}

const char *JNativeAds_Create(const char *instanceId,
                              const char *gameObjectName, const char *unitID,
                              long refresh, float x, float y, float width,
                              float height, const char *templateName,
                              const char *quitType,
                              float paddingStart, float paddingEnd,
                              float paddingTop, float paddingBottom) {
  JNativeAds_Initialize();

  NSString *instanceIdStr = [NSString stringWithUTF8String:instanceId];
  NSString *gameObjectNameStr = [NSString stringWithUTF8String:gameObjectName];
  NSString *unitIDStr = [NSString stringWithUTF8String:unitID];
  NSString *templateStr = [NSString stringWithUTF8String:templateName];
  NSString *quitTypeStr = [NSString stringWithUTF8String:quitType];

  JUnityNativeOverlayCallback *callback = [[JUnityNativeOverlayCallback alloc]
      initWithInstanceId:instanceIdStr
          gameObjectName:gameObjectNameStr];
  callbackInstances[instanceIdStr] = callback;

  JNativeOverlay *overlay =
      [[JNativeOverlay alloc] initWithCallback:callback
                                        unitID:unitIDStr
                                       refresh:(NSTimeInterval)refresh
                                             x:x
                                             y:y
                                         width:width
                                        height:height
                                      template:templateStr
                                      quitType:quitTypeStr
                                  paddingStart:paddingStart
                                    paddingEnd:paddingEnd
                                    paddingTop:paddingTop
                                 paddingBottom:paddingBottom];

  overlayInstances[instanceIdStr] = overlay;


  // Return the instance ID as confirmation
  return strdup([instanceIdStr UTF8String]);
}

void JNativeAds_Load(const char *instanceId) {
  NSString *instanceIdStr = [NSString stringWithUTF8String:instanceId];
  JNativeOverlay *overlay = overlayInstances[instanceIdStr];
  if (overlay) {
    [overlay load];
  } else {
  }
}

void JNativeAds_Show(const char *instanceId) {
  NSString *instanceIdStr = [NSString stringWithUTF8String:instanceId];
  JNativeOverlay *overlay = overlayInstances[instanceIdStr];
  if (overlay) {
    [overlay show];
  } else {
  }
}

void JNativeAds_Hide(const char *instanceId) {
  NSString *instanceIdStr = [NSString stringWithUTF8String:instanceId];
  JNativeOverlay *overlay = overlayInstances[instanceIdStr];
  if (overlay) {
    [overlay hide];
  } else {
  }
}

void JNativeAds_Destroy(const char *instanceId) {
  NSString *instanceIdStr = [NSString stringWithUTF8String:instanceId];
  JNativeOverlay *overlay = overlayInstances[instanceIdStr];
  if (overlay) {
    [overlay destroy];
    [overlayInstances removeObjectForKey:instanceIdStr];
    [callbackInstances removeObjectForKey:instanceIdStr];
  } else {
  }
}

void JNativeAds_OnApplicationPause(const char *instanceId, bool pause) {
  NSString *instanceIdStr = [NSString stringWithUTF8String:instanceId];
  JNativeOverlay *overlay = overlayInstances[instanceIdStr];
  if (overlay) {
    [overlay onApplicationPause:pause];
  }
}

void JNativeAds_SetQuitButtonScale(const char *instanceId, float scale) {
  NSString *instanceIdStr = [NSString stringWithUTF8String:instanceId];
  JNativeOverlay *overlay = overlayInstances[instanceIdStr];
  if (overlay) {
    [overlay setQuitButtonScale:scale];
  } else {
  }
}

void JNativeAds_SetBannerTheme(const char *instanceId, const char *theme) {
  NSString *instanceIdStr = [NSString stringWithUTF8String:instanceId];
  NSString *themeStr = theme ? [NSString stringWithUTF8String:theme] : @"";
  JNativeOverlay *overlay = overlayInstances[instanceIdStr];
  if (overlay) {
    [overlay setBannerTheme:themeStr];
  } else {
  }
}

void JNativeAds_SetRefreshTime(const char *instanceId, long seconds) {
  NSString *instanceIdStr = [NSString stringWithUTF8String:instanceId];
  JNativeOverlay *overlay = overlayInstances[instanceIdStr];
  if (overlay) {
    [overlay setRefreshTime:(NSTimeInterval)seconds];
  } else {
  }
}

} // extern "C"
