// Add this to the scene as a separate GameObject so the ATT prompt is requested on startup.
using UnityEngine;
#if UNITY_IOS
using Unity.Advertisement.IosSupport;
#endif

public class AttPermissionRequest : MonoBehaviour
{
    private void Awake()
    {
#if UNITY_IOS
        // Only prompt while the user's tracking status is still undetermined.
        if (ATTrackingStatusBinding.GetAuthorizationTrackingStatus() == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
        {
            ATTrackingStatusBinding.RequestAuthorizationTracking();
        }
#endif
    }
}