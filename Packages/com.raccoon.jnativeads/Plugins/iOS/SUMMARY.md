﻿iOS Native Ads Plugin — Summary

Overview

This document summarizes the architecture and runtime flow of the iOS native ads plugin integrated with Unity.

Architecture

Unity (C#)
- JNativeAdsManager (C#) calls native functions via DllImport("__Internal").

Bridge Layer (.mm)
- JNativeAdsBridge.mm exports C functions (extern "C"): Create, Load, Show, Hide, Destroy, etc.
- Manages instances in dictionaries keyed by instanceId.
- Implements JUnityNativeOverlayCallback to convert native events into UnitySendMessage calls.

Core Layer (Objective-C)
- JNativeOverlay: central class that loads ads (GADAdLoader), manages lifecycle, and coordinates Layout + QuitMethod.
- Layouts: Based on Google Native Templates (https://developers.google.com/admob/ios/native/templates)
  - JBannerLayout: Small Template (GADTSmallTemplateView) - compact horizontal layout
  - JPortraitLayout: Medium Template Portrait (GADTMediumTemplateView) - media on top
  - JLandscapeLayout: Medium Template Landscape (GADTMediumTemplateView) - media on left
- QuitMethods: JInterClose (countdown), JNativeClose (immediate), JNoneClose (no UI).
- JNativeTemplateStyle: Style configuration for templates (fonts, colors, corner radius, etc.)

Google Mobile Ads SDK
- GADAdLoader, GADNativeAd, GADNativeAdView, GADMediaView used for loading and rendering native ads.

Runtime Flow

1) Create
- Unity → JNativeAds_Create(instanceId, config)
- Bridge creates JNativeOverlay(instance) and callback, stores them in dictionaries.

2) Load
- Unity → JNativeAds_Load(instanceId)
- JNativeOverlay.setupAdLoader() and GADRequest is sent.
- On success: adLoader:didReceiveNativeAd: sets nativeAd, sets paidEventHandler, notifies callback (onLoadSucceed).

3) Show
- Unity → JNativeAds_Show(instanceId)
- JNativeOverlay.show(): createLayout() if needed, layout.renderWithNativeAd(nativeAd), layout.show(), quitMethod.onShow(), start auto-refresh timer if configured.

4) Close/Hide
- User taps close button or nativeOverlay.hide() called → layout.hide(); callback.onClosed(); stop refresh timer.

5) Destroy
- Unity → JNativeAds_Destroy(instanceId)
- JNativeOverlay.destroy(): stop timer, quitMethod.destroy(), layout.destroy(), remove instance.

Key Details and Best Practices

- Thread safety: All UIKit operations are dispatched to the main thread.
- Memory: Callbacks are weak-referenced; invalidate NSTimer in destroy to avoid leaks.
- Multi-instance support: overlayInstances and callbackInstances dictionaries keyed by instanceId allow multiple overlays.
- iOS 13+: Use UIWindowScene to find key window; fallback to deprecated keyWindow for older iOS.
- Bridge: Use @autoreleasepool in C functions and proper C-string/NSString conversion.
- Paid events: Use nativeAd.paidEventHandler to forward GADAdValue via callback and UnitySendMessage.

Files

- Bridge/JNativeAdsBridge.mm — C interface and callback adapter.
- JNativeOverlay.h/.m — Core overlay implementation.
- JNativeOverlayCallback.h — Callback protocol for ad events.
- Protocols/JNativeAdLayout.h — Layout protocol.
- Protocols/JQuitMethod.h — Quit method protocol.
- Layouts/JBaseNativeLayout.h/.m — Base class for all layouts.
- Layouts/JBannerLayout.h/.m — Small Template (compact horizontal).
- Layouts/JPortraitLayout.h/.m — Medium Template Portrait (media on top).
- Layouts/JLandscapeLayout.h/.m — Medium Template Landscape (media on left).
- Layouts/JNativeTemplateStyle.h/.m — Style configuration for templates.
- Layouts/README.md — Detailed documentation for all templates.
- QuitMethods/JInterClose.h/.m — Interstitial-style close (countdown).
- QuitMethods/JNativeClose.h/.m — Native-style close (immediate).
- QuitMethods/JNoneClose.h/.m — No close button.

Template Selection

| templateName | orientation | Layout Class |
|--------------|-------------|--------------|
| "small" | any | JBannerLayout |
| "banner" | any | JBannerLayout |
| "medium" | "portrait" | JPortraitLayout |
| "medium" | "landscape" | JLandscapeLayout |

This file is a concise reference to the plugin's components and runtime flow. Use it when integrating or debugging the Unity ⇄ iOS native bridge and ad rendering logic.
