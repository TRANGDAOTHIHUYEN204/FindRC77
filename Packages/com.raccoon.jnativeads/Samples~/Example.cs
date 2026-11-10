using System.Collections.Generic;
using System.Globalization;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using JKit.Monetize.Ads;
using UnityEngine;
using UnityEngine.UI;
using LoadAdError = JKit.Monetize.Ads.LoadAdError;

public class Example : MonoBehaviour
{
    [Tooltip("Kéo Button UI vào đây để gọi Android Activity")]
    public Button callAndroidActivityButton;

    public RectTransform rectTransform;

    public InputField inputField, inputField2;
    
    private NativeOverlay nativeOverlay;

    void Start()
    {
        // Gán listener cho nút (nếu bạn sử dụng UI Button trong Unity)
        if (callAndroidActivityButton != null)
        {
            callAndroidActivityButton.onClick.AddListener(ShowNative);
            Debug.Log("Button listener assigned.");
        }
        else
        {
            Debug.LogWarning("Call Android Activity Button is not assigned in the Inspector.");
        }

        MobileAds.RaiseAdEventsOnUnityMainThread = true;
        // var request = new ConsentRequestParameters();
        // var debugSettings = new ConsentDebugSettings
        // {
        //     // Geography appears as in EEA for debug devices.
        //     DebugGeography = DebugGeography.EEA,
        //     TestDeviceHashedIds = new List<string> { SystemInfo.deviceUniqueIdentifier },
        // };
        // request.ConsentDebugSettings = debugSettings;
        // ConsentInformation.Reset();
        // ConsentInformation.Update(request, OnConsentInfoUpdated);

        Invoke(nameof(InitAdmob), 1f);
    }

    void InitAdmob()
    {
            MobileAds.Initialize(initStatus =>
            {
                // LoadAd2();
            });
    }

    public void LoadAd2()
    {
        if (nativeOverlay != null)
        {
            nativeOverlay.Hide();
            nativeOverlay.Destroy();
        }
        //change orien landscape to portrait
        // Screen.orientation = ScreenOrientation.Portrait;
        // NativeOverlay.Load("ca-app-pub-3940256099942544/2247696110", 30, Handle);
        // NativeOverlay.Load(
        //     new NativeOptions.Builder()
        //         .PositionAndSize(rectTransform, true)
        //         .Orientation(NativeOptions.TEMPLATE_LANDSCAPE) // tải về quảng cáo dạng ngang
        //         .Template(NativeOptions.TEMPLATE_BANNER) // hiên thị theo template dạng ads ngang
        //         .Quit(NativeOptions.NONE_QUIT)
        //         .Refresh(30)
        //         .Build("ca-app-pub-3940256099942544/2247696110"),
        //     // .Build("ca-app-pub-8190506959251235/2043096362"),
        //     Handle
        // );
        
        var _cachedPaddingX = Screen.width  - Screen.safeArea.xMax;
        var _cachedPaddingY = Screen.height - Screen.safeArea.yMax;
        var padding_x = _cachedPaddingX;
        var padding_y = _cachedPaddingY;
        var builder = new NativeOptions.Builder()
            .Template(NativeOptions.AutoTemplate())
            .PositionAndSize(0, 0, Screen.width, Screen.height)
            .Orientation(NativeOptions.AutoOrientation())
            .Quit(NativeOptions.INTER_QUIT_2, padding_y, padding_x)
            .Refresh(30)
            .SetPauseOnShow(true)
            .Build("ca-app-pub-3940256099942544/2247696110");

        NativeOverlay.Load(builder, Handle);
    }

    public void LoadAd()
    {
        if (nativeOverlay != null)
        {
            nativeOverlay.Hide();
            nativeOverlay.Destroy();
        }
        //change orien landscape to portrait
        // Screen.orientation = ScreenOrientation.Portrait;
        NativeOverlay.Load("ca-app-pub-3940256099942544/2247696110", 15, Handle);
        // NativeOverlay.Load(
        //     new NativeOptions.Builder()
        //        .PositionAndSize(rectTransform)
        //        .Orientation(NativeOptions.TEMPLATE_PORTRAIT) // tải về quảng cáo dạng ngang
        //        .Template(NativeOptions.TEMPLATE_PORTRAIT)          // hiên thị theo template dạng ads ngang
        //        .Quit(NativeOptions.NATIVE_QUIT)
        //        .Refresh(10)
        //        .Build("ca-app-pub-3940256099942544/2247696110"),
        //        // .Build("ca-app-pub-8190506959251235/2043096362"),
        //     Handle
        // );
    }
    
    async void Handle(NativeOverlay overlay, LoadAdError error)
    {
        if (overlay != null && error == null)
        {
            nativeOverlay = overlay;
            Debug.Log("[Example] ✅ NativeOverlay loaded successfully.");
            // Register events
            nativeOverlay.OnClosed += () => { Debug.Log("[Example] 🔴 NativeOverlay closed."); nativeOverlay?.Destroy(); nativeOverlay = null; };
            nativeOverlay.OnClicked += () => { Debug.Log("[Example] 👆 NativeOverlay clicked."); };
            nativeOverlay.OnImpression += () => { Debug.Log("[Example] 👁️ NativeOverlay impression recorded."); };
            nativeOverlay.OnAdPaid += value => { Debug.Log($"[Example] 💰 Ad Paid: {value.Value}"); };
            var canParse = float.TryParse(inputField.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float scale);
            if (!canParse)
                scale = 1f;
            
            canParse = float.TryParse(inputField2.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float scaleCover);
            if (!canParse)
                scaleCover = 1f;
            Debug.Log($"[Example] inputField.text : {inputField.text} - {scale} -  {scaleCover}");
            
            nativeOverlay.SetScaleButtonQuit(scale);
            // nativeOverlay.SetScaleButtonQuitCover(scaleCover);
            // Auto show
            Debug.Log("[Example] Calling Show() to display ad...");
            nativeOverlay.Show();
            Debug.Log("[Example] ✅ Show() called - Ad should be visible now!");
        }
        else
        {
            Debug.LogWarning($"[Example] ❌ Failed to load NativeOverlay: {error?.Message ?? "Unknown error"}");
            await Awaitable.WaitForSecondsAsync(1);
            await Awaitable.MainThreadAsync();
            nativeOverlay?.Destroy();
            nativeOverlay = null;
            Debug.Log("[Example] Retrying LoadAd()...");
            LoadAd();
        }
    }

    private void OnConsentInfoUpdated(FormError consentError)
    {
        Debug.Log("[Example] Consent Info Updated.");
        
        if (consentError != null)
        {
            Debug.LogWarning("[Example] consent error: " + consentError);
            // MobileAds.Initialize(initStatus => { LoadAd(); });
            return;
        }

        ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
        {
            Debug.Log("[Example] Load And Show Consent Form.");
            if (formError != null)
            {
                Debug.LogWarning(formError);
                return;
            }

            if (ConsentInformation.CanRequestAds())
            {
                Debug.Log("[Example] CanRequestAds.");
                // MobileAds.Initialize(initStatus => { LoadAd(); });
            }
        });
    }

    public void ShowNative()
    {
        if (nativeOverlay != null)
        {
            nativeOverlay.OnClosed += () => { Debug.Log("NativeOverlay closed."); };
            nativeOverlay.OnClicked += () => { Debug.Log("NativeOverlay clicked."); };
            nativeOverlay.OnImpression += () => { Debug.Log("NativeOverlay impression."); };
            nativeOverlay.OnAdPaid += value => { Debug.Log("On Ad Paid"); };
            nativeOverlay.Show();
        }
        else
        {
                // MobileAds.Initialize(initStatus => { LoadAd(); });
        }
    }
    
}