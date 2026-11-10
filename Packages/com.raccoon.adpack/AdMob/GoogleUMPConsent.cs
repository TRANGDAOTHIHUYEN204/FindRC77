using System;
using System.Collections.Generic;
using System.Threading;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace RCKit.Ads.AdMob
{
    public class GoogleUMPConsent : IConsentProvider
    {
        public event Action OnDismissed;

        public DebugGeography debugGeography = DebugGeography.EEA;
        public List<string> testDeviceHashedIds;

        // Fallback in case Update/ConsentForm.Load never calls back (observed on
        // some devices), which would otherwise stall the game forever. Only the
        // network stages (Update, Load) are timed; once the form is on screen the
        // user decides, so no timeout runs while it is shown.
        // A timeout fires OnDismissed early (AdsManager will not init ads while
        // CanRequestAds() is false), but the flow keeps going: a late Update/Load
        // callback still loads and shows the form, then OnDismissed fires again so
        // ads init with a valid TC string. OnDismissed fires at most twice per
        // request: once on timeout, once on real completion.
        private const float UpdateTimeoutSeconds = 8f;
        private const float LoadFormTimeoutSeconds = 8f;

        // Retry khi flow kết thúc mà CanRequestAds() vẫn false (vd user mới cài, mở app
        // lúc offline => Update lỗi). Không retry thì cả phiên đó không có ads, kể cả
        // user ngoài EEA. Chỉ retry khi app resume hoặc mạng có lại (không poll liên tục
        // để tránh form consent bật lên giữa màn chơi), dừng hẳn khi CanRequestAds() true.
        private const float RetryNetworkPollSeconds = 5f;
        private const float RetryMinIntervalSeconds = 10f;

        private bool requestInFlight;
        private bool timeoutFired;
        private CancellationTokenSource timeoutCts;
        private CancellationTokenSource retryCts;
        private float lastRequestTime = float.NegativeInfinity;

        public void RequestConsent()
        {
            if (requestInFlight) return;
            requestInFlight = true;
            lastRequestTime = Time.realtimeSinceStartup;
            timeoutFired = false;
            StartWatchdog(UpdateTimeoutSeconds);

            var request = new ConsentRequestParameters();
#if DEBUG_GOOGLE_UMP_CONSENT
            var debugSettings = new ConsentDebugSettings
            {
                // Geography appears as in EEA for debug devices.
                DebugGeography = debugGeography,
            };

            if (testDeviceHashedIds != null)
                debugSettings.TestDeviceHashedIds = testDeviceHashedIds;
            request.ConsentDebugSettings = debugSettings;
            ConsentInformation.Reset();
#endif
            ConsentInformation.Update(request, OnConsentInfoUpdated);
        }

        private void StartWatchdog(float seconds)
        {
            timeoutCts?.Cancel();
            timeoutCts = new CancellationTokenSource();
            WatchForTimeout(seconds, timeoutCts.Token, OnWatchdogTimeout);
        }

        private async void WatchForTimeout(float seconds, CancellationToken token, Action callback)
        {
            try
            {
                await Awaitable.WaitForSecondsAsync(seconds, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (token.IsCancellationRequested) return;
            Debug.LogWarning("[GoogleUMPConsent] Timed out waiting for consent callback, resuming game; consent flow keeps running.");
            callback?.Invoke();
        }

        private async void OnConsentInfoUpdated(FormError consentError)
        {
            await Awaitable.MainThreadAsync();
            if (!requestInFlight) return;

            if (consentError != null)
            {
                Debug.LogWarning(consentError.Message);
                Complete();
                return;
            }

            if (ConsentInformation.ConsentStatus != ConsentStatus.Required
                || !ConsentInformation.IsConsentFormAvailable())
            {
                Complete();
                return;
            }

            // Sau timeout thì không bật watchdog nữa: game đã chạy tiếp, chỉ chờ form.
            if (!timeoutFired) StartWatchdog(LoadFormTimeoutSeconds);
            ConsentForm.Load(OnConsentFormLoaded);
        }

        private async void OnConsentFormLoaded(ConsentForm form, FormError loadError)
        {
            await Awaitable.MainThreadAsync();
            if (!requestInFlight) return;

            // Form is ready; from here on the user decides, so stop the watchdog.
            timeoutCts?.Cancel();

            if (loadError != null || form == null)
            {
                Debug.LogWarning(loadError?.Message);
                Complete();
                return;
            }

            form.Show(OnConsentDismissed);
        }

        public async void OnConsentDismissed(FormError formError)
        {
            await Awaitable.MainThreadAsync();
            if (formError != null)
                Debug.LogWarning(formError.Message);

            Complete();
        }

        private void OnWatchdogTimeout()
        {
            if (!requestInFlight || timeoutFired) return;
            timeoutFired = true;
            OnDismissed?.Invoke();
        }

        // Kết thúc thật của flow consent (đã có status cuối / lỗi / form đóng).
        private void Complete()
        {
            if (!requestInFlight) return;
            requestInFlight = false;
            timeoutCts?.Cancel();

            if (ConsentInformation.CanRequestAds()) DisarmRetry();
            else ArmRetry();

            OnDismissed?.Invoke();
        }

        private void ArmRetry()
        {
            if (retryCts != null) return;
            retryCts = new CancellationTokenSource();
            Application.focusChanged += OnApplicationFocusChanged;
            WatchNetworkForRetry(retryCts.Token);
        }

        private void DisarmRetry()
        {
            if (retryCts == null) return;
            retryCts.Cancel();
            retryCts = null;
            Application.focusChanged -= OnApplicationFocusChanged;
        }

        private void OnApplicationFocusChanged(bool hasFocus)
        {
            if (hasFocus) TryRetry("app resumed");
        }

        private async void WatchNetworkForRetry(CancellationToken token)
        {
            var wasOffline = Application.internetReachability == NetworkReachability.NotReachable;
            while (true)
            {
                try
                {
                    await Awaitable.WaitForSecondsAsync(RetryNetworkPollSeconds, token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                if (token.IsCancellationRequested) return;
                var isOffline = Application.internetReachability == NetworkReachability.NotReachable;
                if (wasOffline && !isOffline) TryRetry("network reachable");
                wasOffline = isOffline;
            }
        }

        private void TryRetry(string reason)
        {
            if (requestInFlight) return;
            if (ConsentInformation.CanRequestAds())
            {
                DisarmRetry();
                return;
            }
            if (Application.internetReachability == NetworkReachability.NotReachable) return;
            // Chặn retry dồn dập (vd focus đổi liên tục khi form/activity khác bật lên).
            if (Time.realtimeSinceStartup - lastRequestTime < RetryMinIntervalSeconds) return;

            Debug.Log($"[GoogleUMPConsent] Retrying consent request ({reason}).");
            RequestConsent();
        }

        public bool CanRequestAds() => ConsentInformation.CanRequestAds();

        // Required by Google UMP policy: when true, the game must expose an entry
        // point (e.g. a "Privacy options" button in Settings) that calls
        // ShowPrivacyOptionsForm so users can change their consent.
        public bool IsPrivacyOptionsRequired =>
            ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

        public void ShowPrivacyOptionsForm(Action<FormError> onDone = null)
        {
            ConsentForm.ShowPrivacyOptionsForm(async formError =>
            {
                await Awaitable.MainThreadAsync();
                if (formError != null)
                    Debug.LogWarning(formError.Message);
                onDone?.Invoke(formError);
            });
        }
    }
}