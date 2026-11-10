# JNative Ads

Native Android & iOS overlay ads integration for Unity, built on Google Mobile Ads SDK. Ads are rendered as native platform overlays (Android `View` / iOS `UIView`) on top of the Unity surface, not inside the Unity render pipeline.

## Installation

- This package is currently distributed in-place under `Assets/JNativeAds/`. Future versions may be published as a Unity Package Manager git package.
- `git UPM add https://bitbucket.org/tripglobal68/jnativeadsplugin.git#release`

## Requirements

- Unity 2021.3 or later
- Google Mobile Ads Unity SDK (imported separately as a `.unitypackage`)
- External Dependency Manager for Unity (EDM4U) — handles Android/iOS native dependencies via `Editor/JNativeAdsDependencies.xml`

## Quick Start

```csharp
using JKit.Monetize.Ads;

NativeOverlay.Load(
    new NativeOptions.Builder()
        .SetOptionFullScreen(NativeOptions.TEMPLATE_FULLSCREEN2)
        .Quit(NativeOptions.INTER_QUIT_3)
        .Refresh(30)               // seconds; -1 disables auto-refresh
        .SetPauseOnShow(true)      // pause Time.timeScale / audio while shown
        .SetSupportMedia(true)
        .SetTimeClose(3)           // seconds before the quit button becomes tappable
        .SetReloadOnClick(true)
        .Build("ca-app-pub-xxxxxxxxxxxxxxxx/yyyyyyyyyy"),
    OnNativeOverlayLoaded);

void OnNativeOverlayLoaded(NativeOverlay overlay, LoadAdError error)
{
    if (error != null)
    {
        Debug.LogWarning($"Load failed: {error.Message}");
        return;
    }

    overlay.OnImpression += () => Debug.Log("Impression");
    overlay.OnClicked    += () => Debug.Log("Clicked");
    overlay.OnClosed     += () => { overlay.Destroy(); };
    overlay.OnAdPaid     += value => Debug.Log($"Paid: {value.Value}");

    overlay.Show();
}
```

Call `overlay.Hide()` / `overlay.Destroy()` when done, and `overlay.OnResetEvent()` to clear all subscribed callbacks without destroying the instance.

There are also two convenience overloads of `NativeOverlay.Load(...)` for common full-screen setups — see `Runtime/NativeOverlay.cs`.

## Templates (`NativeOptions.TEMPLATE_*`)

| Constant | Layout |
| --- | --- |
| `TEMPLATE_BANNER` | Banner strip |
| `TEMPLATE_PORTRAIT` | Portrait native ad |
| `TEMPLATE_LANDSCAPE` | Landscape native ad |
| `TEMPLATE_FULLSCREEN` | Full-screen native ad |
| `TEMPLATE_FULLSCREEN2` | Full-screen native ad, alternate layout |
| `TEMPLATE_FULLSCREEN_MEDIA_2` | Full-screen with media, alternate layout |
| `TEMPLATE_LANDSCAPE_FAN` | Landscape, FAN-friendly layout |
| `TEMPLATE_LANDSCAPE_BANNER` | Landscape banner layout |

`NativeOptions.AutoTemplate()` picks `portrait`/`landscape` based on the current `Screen.orientation` when no explicit template is given.

## Quit (close) button types (`NativeOptions.*_QUIT`)

| Constant | Behavior |
| --- | --- |
| `INTER_QUIT`, `INTER_QUIT_2`, `INTER_QUIT_3` | Interstitial-style close button with a countdown before it becomes active |
| `NATIVE_QUIT` | Native-style close button, no countdown |
| `NONE_QUIT` | No close button |

Tune the close button with:
- `overlay.SetScaleButtonQuit(float)` — clamped to `0.1`–`1`
- `overlay.SetScaleButtonQuitCover(float)` — clamped to `0.5`–`2` (Android/iOS tap-area cover scale)
- `builder.SetTimeClose(int)` — seconds before the button becomes tappable

## Other configuration

- `overlay.SetPauseOnShow(bool)` — pause/resume `Time.timeScale` and `AudioListener` while the ad is shown
- `overlay.SetBannerTheme(NativeOptions.BANNER_THEME_LIGHT | BANNER_THEME_DARK)` — Android banner theme (default: dark)
- `overlay.SetRefreshTime(long seconds)` — Android banner refresh interval
- `overlay.SetEnforceMinViewTimeBeforeHide(bool)` — Android, require a minimum view time before the ad can be hidden (show-rate optimization)
- `builder.SetReloadOnClick(bool)` — reload the ad automatically after a click
- `builder.SetSupportMedia(bool)` — request media assets with the native ad

## Callbacks

`NativeOverlay` exposes `OnImpression`, `OnClicked`, `OnClosed` and `OnAdPaid` events, all raised on the Unity main thread. Load failures are reported via the `LoadAdError error` parameter of the `Load(...)` callback instead of an event.

## Sample

A sample scene is included under `Samples/Main.unity` along with `Samples/Example.cs`.

## Changelog

See [CHANGELOG.md](./CHANGELOG.md).

## License

Proprietary — Raccoon.
