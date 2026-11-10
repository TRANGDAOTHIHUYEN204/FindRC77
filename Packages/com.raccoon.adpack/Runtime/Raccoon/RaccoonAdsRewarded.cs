using System;
using UnityEngine;

namespace RCKit.Ads.Raccoon
{
    // RaccoonAdsRewardedVideo là một wrapper cho IAdsRewardedVideo, tự động reload khi load thất bại và sau khi show xong.
    public class RaccoonAdsRewarded : IAdsRewarded, IAdsUnit
    {
        private bool reload_after_show = false;
        private bool reload_on_fail = true;
        private bool _cancelNextReload;

        public event Action OnAdShow = delegate { };
        public event Action OnLoadFailed = delegate { };
        public event Action OnLoadSucceed = delegate { };
        public event Action OnClosed = delegate { };

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
        private IAdsRewarded _rewarded;

        public RaccoonAdsRewarded(IAdsRewarded rewarded, float reloadInterval = 1f)
        {
            _rewarded = rewarded;
            _reloadInterval = reloadInterval;
            rewarded.OnLoadFailed += OnAdsLoadFailed;
            rewarded.OnLoadSucceed += OnAdsLoadSucceed;
            rewarded.OnClosed += OnClosedAds;
        }

        ~RaccoonAdsRewarded()
        {
            _rewarded.OnLoadFailed -= OnAdsLoadFailed;
            _rewarded.OnLoadSucceed -= OnAdsLoadSucceed;
            _rewarded.OnClosed -= OnClosedAds;
        }

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
            _rewarded.Load();
        }

        public void Load()
        {
            _cancelNextReload = false;
            _rewarded.Load();
        }

        public bool IsReady() => _rewarded.IsReady();

        public void Show(Action onRewarded, Action onFailed)
        {
            OnAdShow();
            UnityMainThreadDispatcher.Enqueue(() => _rewarded.Show(
                () =>
                {
                    onRewarded?.Invoke();
                    // Tự động reload sau khi show xong để sẵn sàng cho lần tiếp theo.
                    if (reload_after_show)
                        _rewarded.Load();
                },
                () =>
                {
                    onFailed?.Invoke();
                    _rewarded.Load();
                }
            ));
        }

        public void Destroy() => _rewarded.Destroy();
        public void SetRefreshTime(long refreshTime)
        {
            _rewarded?.SetRefreshTime(refreshTime);
        }

        public void SetQuitButtonScale(float scale)
        {
            _rewarded?.SetQuitButtonScale(scale);
        }

        public void SetReloadAfterShow()
        {
            reload_after_show = true;
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

        private void OnClosedAds()
        {
            OnClosed();
        }
    }
}