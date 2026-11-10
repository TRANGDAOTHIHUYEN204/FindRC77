using System;

namespace RCKit.Ads.Max
{
    public class MaxMediation : IMediationProvider
    {
        public event Action OnMediationInitializeCompleted;
        public event Action<MaxSdkBase.AdInfo> OnPaid;

        public MaxMediation()
        {
            MaxSdkCallbacks.OnSdkInitializedEvent += OnSdkInitialized;
            // AppLovin MAX revenue callbacks là global static events, subscribe một lần ở Mediation level.
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnAdsPaid;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnAdsPaid;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnAdsPaid;
            MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += OnAdsPaid;
        }

        ~MaxMediation()
        {
            MaxSdkCallbacks.OnSdkInitializedEvent -= OnSdkInitialized;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent -= OnAdsPaid;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent -= OnAdsPaid;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent -= OnAdsPaid;
            MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent -= OnAdsPaid;
        }

        public void Initialize()
        {
            MaxSdk.InitializeSdk();
        }

        private void OnSdkInitialized(MaxSdkBase.SdkConfiguration config)
        {
            // One-shot: unsubscribe ngay sau khi init xong.
            OnMediationInitializeCompleted?.Invoke();
        }

        private void OnAdsPaid(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            OnPaid?.Invoke(adInfo);
        }
    }
}