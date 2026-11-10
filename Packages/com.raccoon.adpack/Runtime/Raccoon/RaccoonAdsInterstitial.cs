using System;
using UnityEngine;

namespace RCKit.Ads.Raccoon
{
    // Interstitial Ads Wrapper for RaccoonAds. It will automatically reload the ad after failed to load.
    public class RaccoonAdsInterstitial : IAdsUnit, IAdsInterstitial, IAdsRemovable
    {
        private bool reload_after_show = false;
        private bool reload_on_fail = true;
        private bool _cancelNextReload;
        
        public event Action OnAdShow = delegate { };
        public event Action OnLoadFailed = delegate { };
        public event Action OnLoadSucceed = delegate { };
        public event Action OnClosed = delegate { };

        private IAdsInterstitial _interstitial;
        private float _reloadInterval;
        // Số lần load fail liên tiếp — giãn delay reload: base, x2, x4... tối đa MaxReloadDelay.
        private int _consecutiveFails;
        private const float MaxReloadDelay = 15f;

        private float NextReloadDelay()
        {
            var delay = Mathf.Min(_reloadInterval * (1 << Mathf.Min(_consecutiveFails, 10)), MaxReloadDelay);
            _consecutiveFails++;
            return delay;
        }

        public RaccoonAdsInterstitial(IAdsInterstitial interstitial, float reloadInterval = 1f)
        {
            _interstitial = interstitial;
            _reloadInterval = reloadInterval;
            interstitial.OnLoadFailed += OnAdsLoadFailed;
            interstitial.OnLoadSucceed += OnAdsLoadSucceed;
            interstitial.OnClosed += OnClosedAds;
        }

        ~RaccoonAdsInterstitial()
        {
            _interstitial.OnLoadFailed -= OnAdsLoadFailed;
            _interstitial.OnLoadSucceed -= OnAdsLoadSucceed;
            _interstitial.OnClosed -= OnClosedAds;
        }

        public void Destroy() => _interstitial.Destroy();
        public void SetRefreshTime(long refreshTime)
        {
            _interstitial?.SetRefreshTime(refreshTime);
        }

        public void SetQuitButtonScale(float scale)
        {
            _interstitial?.SetQuitButtonScale(scale);
        }

        public void SetQuitButtonCoverScale(float scale)
        {
            _interstitial?.SetQuitButtonCoverScale(scale);
        }
        
        public void SetReloadAfterShow(bool reloadAfterShow)
        {
            reload_after_show = reloadAfterShow;
        }

        public void SetReloadOnFail(bool reloadOnFail)
        {
            reload_on_fail = reloadOnFail;
        }

        /// <summary>
        /// Nếu đang có 1 lượt load dở (in-flight): để nó chạy tiếp bình thường, nhưng nếu lượt đó fail
        /// thì không tự reload lại nữa (chỉ áp dụng 1 lần cho lượt fail kế tiếp). Nếu lượt đó load thành
        /// công thì không ảnh hưởng gì, ad vẫn cache sẵn sàng như bình thường.
        /// </summary>
        public void CancelReloadOnFail()
        {
            _cancelNextReload = true;
        }

        void IAdsRemovable.OnRemoved() => ((IAdsRemovable)_interstitial).OnRemoved();

        private void OnAdsLoadSucceed()
        {
            _consecutiveFails = 0;
            OnLoadSucceed();
        }

        private async void OnAdsLoadFailed()
        {
            await Awaitable.MainThreadAsync();
            OnLoadFailed();
            if (_cancelNextReload)
            {
                _cancelNextReload = false;
                return;
            }
            if (!reload_on_fail) return;
            await Awaitable.WaitForSecondsAsync(NextReloadDelay());
            _interstitial.Load();
        }

        public void Load()
        {
            Debug.Log("RaccoonAdsInterstititial.Load()");

            _cancelNextReload = false;
            _interstitial.Load();
        }

        public bool IsReady() => _interstitial.IsReady();

        public void Show(Action<bool> onCallback)
        {
            OnAdShow();
            Action<bool> callback = null;
            callback += onCallback;
            if (reload_after_show)
                callback += _ => Load();
            Debug.Log("RaccoonAdsInterstitial.Show()");

            UnityMainThreadDispatcher.Enqueue(() => _interstitial.Show(callback));
        }

        private void OnClosedAds()
        {
            Debug.Log("RaccoonAdsInterstitial : Close");
            OnClosed();
        }
    }
}