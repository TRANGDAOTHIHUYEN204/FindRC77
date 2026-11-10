
using System;

namespace RCKit.Ads
{
    public class AdsManager
    {
        public readonly IConsentProvider consent;
        public IConfigProvider[] configs;
        public IMediationProvider[] mediations;
        public IAdsRemovable[] removables;
        private bool adsRemoved;
        public Action onRequestShowATT;

        public AdsManager(IConsentProvider consent, bool adsRemoved = false)
        {
            this.adsRemoved = adsRemoved;
            this.consent = consent;
            consent.OnDismissed += OnDismissed;
        }

        ~AdsManager()
        {
            consent.OnDismissed -= OnDismissed;
        }


        public bool AdsRemoved
        {
            get => adsRemoved;
            set
            {
                if (adsRemoved) return;
                adsRemoved = value;
                foreach (var removable in removables)
                    removable.OnRemoved();
            }
        }

        public void Initialize()
        {
            foreach (var config in configs)
                config.RunConfig();
            consent.RequestConsent();
        }

        private bool mediationInitialized;

        // Consent provider có thể bắn OnDismissed 2 lần (timeout rồi mới xong thật).
        // Chỉ init SDK khi đã được phép request ad (EEA/UK/CH phải có TC string),
        // nếu không request sẽ thiếu TC string => AdMob hạn chế quảng cáo cá nhân hoá.
        private void OnDismissed()
        {
            if (mediationInitialized) return;
            if (!consent.CanRequestAds()) return;
            mediationInitialized = true;

            if (IsPlatformIOS)
                onRequestShowATT?.Invoke();

            foreach (var mediation in mediations)
                mediation.Initialize();
        }

        public static bool IsPlatformIOS
        {
            get
            {
                #if UNITY_IOS
                return true;
                #endif
                return false;
            }
        }
    }
}