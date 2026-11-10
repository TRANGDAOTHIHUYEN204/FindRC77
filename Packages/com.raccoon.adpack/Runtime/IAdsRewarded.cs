using System;

namespace RCKit.Ads
{
    public interface IAdsRewarded
    {
        event Action OnAdShow;
        
        event Action OnLoadFailed;
        event Action OnLoadSucceed;
        event Action OnClosed;
        
        void Load();
        bool IsReady();
        void Show(Action onRewarded, Action onFailed);
        void Destroy();
        
        void SetRefreshTime(long refreshTime);

        void SetQuitButtonScale(float scale);

    }
}