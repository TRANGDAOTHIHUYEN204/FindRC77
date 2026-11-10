#if UNITY_IOS
using Unity.Advertisement.IosSupport;
#endif

using UnityEngine;
using System.Collections;

namespace Raccoon.iOS
{
    public class ATTrackingManager
    {
#if UNITY_IOS
        
        public static void ShowRequest()
        {
            // check with iOS to see if the user has accepted or declined tracking
            var status = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
            if (status == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
            {
                Debug.Log("do not author");
                ATTrackingStatusBinding.RequestAuthorizationTracking();
            }
        }
#else
        public static void ShowRequest()
        {
            Debug.Log("Platform != IOS do not need author");
            
        }
#endif
        
    }
}