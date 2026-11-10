using System;

namespace RCKit.Ads
{
    public interface IAdsInterstitial
    {
        event Action OnAdShow;
        
        event Action OnLoadFailed;
        event Action OnLoadSucceed;
        event Action OnClosed;

        void Load();
        // onCallback(isSuccess): true nếu show/close bình thường, false nếu thất bại (chưa ready hoặc display failed).
        void Show(Action<bool> onCallback);
        bool IsReady();
        void Destroy();
        
        void SetRefreshTime(long refreshTime);

        void SetQuitButtonScale(float scale);
        
        void SetQuitButtonCoverScale(float scale);
    }
}