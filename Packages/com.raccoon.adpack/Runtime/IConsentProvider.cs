using System;

namespace RCKit.Ads
{
    public interface IConsentProvider
    {
        event Action OnDismissed;
        void RequestConsent();
        bool CanRequestAds();
    }
}