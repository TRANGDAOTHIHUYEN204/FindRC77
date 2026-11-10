using System;
using UnityEngine;
using GoogleMobileAds.Api;
#if UNITY_ANDROID

namespace JKit.Monetize.Ads
{
    internal class AndroidNativeOverlay : AndroidJavaProxy, INativeOverlay
    {
        private AndroidJavaObject nativeOverlayAd;

        public AndroidNativeOverlay(
            string unitID,
            long refresh,
            float x, float y, float width, float height,
            string template, string quitType,
            float paddingStart, float paddingEnd, float paddingTop, float paddingBottom, bool supportMedia, int timeClose, bool reloadOnClick
        ) : base("com.jackie.jnativeads.UnityNativeOverlayCallback")
        {
            nativeOverlayAd = new AndroidJavaObject(
                "com.jackie.jnativeads.UnityNativeOverlay", this,
                unitID, refresh,
                x, y, width, height,
                template, quitType,
                paddingStart, paddingEnd, paddingTop, paddingBottom, supportMedia, timeClose, reloadOnClick
            );
        }

        public event Action OnImpression;
        public event Action OnClicked;
        public event Action OnClosed;
        public event Action OnLoadSucceed;
        public event Action<AdValue> OnAdPaid;
        public event Action<LoadAdError> OnLoadFailed;

        public void Load() => nativeOverlayAd?.Call("load");
        public void Show() => nativeOverlayAd?.Call("show");
        public void Hide() => nativeOverlayAd?.Call("hide");

        public void Destroy()
        {
            if (nativeOverlayAd == null) return;
            nativeOverlayAd.Call("destroy");
            // Java proxy giữ GCHandle tới object C# này, object C# lại giữ global ref tới Java
            // UnityNativeOverlay → vòng tham chiếu chéo không GC nào phá được. Phải Dispose global
            // ref ở đây, nếu không mỗi lần load sẽ rò nguyên 1 overlay (layout, view, bitmap).
            nativeOverlayAd.Dispose();
            nativeOverlayAd = null;
        }

        void INativeOverlay.OnApplicationPause(bool pauseStatus) => nativeOverlayAd?.Call("onApplicationPause", pauseStatus);

        // use for fullscreen
        public void setQuitButtonScale(float scale) => nativeOverlayAd?.Call("setQuitButtonScale", scale);
        public void setQuitButtonCoverScale(float scale) => nativeOverlayAd?.Call("setQuitButtonCoverScale", scale);

        // use for banner only ( default is dark theme)
        public void setBannerTheme(string theme) => nativeOverlayAd?.Call("setBannerTheme", theme);

        public void setRefreshTime(long seconds) => nativeOverlayAd?.Call("setRefreshTime", seconds);

        public void setEnforceMinViewTimeBeforeHide(bool enable) => nativeOverlayAd?.Call("setEnforceMinViewTimeBeforeHide", enable);

        private async void onLoadSucceed()
        {
            await Awaitable.MainThreadAsync();
            OnLoadSucceed?.Invoke();
        }

        private async void onLoadFailed(AndroidJavaObject error)
        {
            await Awaitable.MainThreadAsync();
            OnLoadFailed?.Invoke(new LoadAdError(error));
        }

        private async void onClosed()
        {
            await Awaitable.MainThreadAsync();
            OnClosed?.Invoke();
        }

        private async void onClicked()
        {
            await Awaitable.MainThreadAsync();
            OnClicked?.Invoke();
        }

        private async void onImpression()
        {
            await Awaitable.MainThreadAsync();
            OnImpression?.Invoke();
        }

        private async void onAdPaid(AndroidJavaObject adValueObject)
        {
            await Awaitable.MainThreadAsync();

            try
            {
                var adValue = new AdValue();
                adValue.CurrencyCode = adValueObject.Call<string>("getCurrencyCode");
                adValue.Value = adValueObject.Call<long>("getValueMicros");
                adValue.Precision = (AdValue.PrecisionType)adValueObject.Call<int>("getPrecisionType");

                OnAdPaid?.Invoke(adValue);
            }
            catch (Exception e)
            {
                // ignored
            }
        }
    }
}
#endif