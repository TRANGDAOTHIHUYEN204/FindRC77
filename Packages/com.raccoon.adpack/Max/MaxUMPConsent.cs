using System;
using System.Threading;
using UnityEngine;

namespace RCKit.Ads.Max
{
    public class MaxUMPConsent : IConsentProvider
    {
        public event Action OnDismissed = delegate { };

        // Fallback in case ShowCmpForExistingUser never calls back, which would
        // otherwise stall ad init forever.
        private const float TimeoutSeconds = 10f;

        private bool dismissedFired;
        private CancellationTokenSource timeoutCts;

        public void RequestConsent()
        {
            dismissedFired = false;
            timeoutCts?.Cancel();
            timeoutCts = new CancellationTokenSource();
            WatchForTimeout(timeoutCts.Token);

            // AppLovin MAX hiển thị CMP form thông qua CmpService.
            // Với người dùng mới, form sẽ tự động hiển thị trong quá trình MaxSdk.InitializeSdk().
            // Ở đây gọi ShowCmpForExistingUser để đảm bảo form hiển thị khi cần re-request.
            MaxSdk.CmpService.ShowCmpForExistingUser(OnCmpDismissed);
        }

        private async void WatchForTimeout(CancellationToken token)
        {
            try
            {
                await Awaitable.WaitForSecondsAsync(TimeoutSeconds, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (token.IsCancellationRequested) return;
            Debug.LogWarning("[MaxUMPConsent] Timed out waiting for CMP callback, resuming ad init.");
            FireOnDismissed();
        }

        private void OnCmpDismissed(MaxCmpError error)
        {
            if (error != null)
                Debug.LogError($"[MaxUMPConsent] CMP error: {error.Message} (code={error.Code})");

            FireOnDismissed();
        }

        private void FireOnDismissed()
        {
            if (dismissedFired) return;
            dismissedFired = true;
            timeoutCts?.Cancel();
            OnDismissed();
        }

        public bool CanRequestAds()
        {
            // AppLovin MAX không cung cấp API CanRequestAds tương đương Google UMP.
            // SDK sẽ tự xử lý consent nội bộ; mặc định cho phép request ads sau khi CMP hoàn tất.
            return true;
        }
    }
}