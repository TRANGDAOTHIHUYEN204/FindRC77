using System;

namespace RCKit.Ads
{
    public interface IMediationProvider
    {
        event Action OnMediationInitializeCompleted;
        void Initialize();
    }
}