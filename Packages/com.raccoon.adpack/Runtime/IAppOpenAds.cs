using System;

namespace RCKit.Ads
{
    public interface IAppOpenAds
    {
        void Initialize();
        
        event Action OnAdShow;
        event Action OnLoadFailed;
        event Action OnLoadSucceed;
        event Action OnClosed;

        void Load();
        void Show(Action onCallback);
        bool IsReady();
        void Destroy();
        
        void SetRefreshTime(long refreshTime);

        void SetQuitButtonScale(float scale);
    }
}
