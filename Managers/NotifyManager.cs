using System;
using System.Collections;
using UnityEngine;

#if UNITY_ANDROID
using Unity.Notifications.Android;
using UnityEngine.Android;
#endif

#if UNITY_IOS
using Unity.Notifications.iOS;
#endif

public class NotifyManager : MonoBehaviour
{
    [Serializable]
    public class NotificationItem
    {
        public string title;

        [TextArea(2, 4)]
        public string message;

        [Tooltip("Hours after which the notification will be sent. You can use decimal values for minutes (e.g., 0.5f = 30 minutes).")]
        public float fireTimeInHours;

        [Tooltip("If enabled, the notification will be sent. If disabled, it will be ignored.")]
        public bool enabled = true;
    }

    [Header("Notify List")]

    [SerializeField]
    private NotificationItem[] notifications = new NotificationItem[]
    {
        new NotificationItem { title = "TEST", message = "Test notify", fireTimeInHours =1f, enabled = true },
        new NotificationItem { title = "Come back!", message = "Come back we miss!", fireTimeInHours = 6f, enabled = false },
    };

    /////////////////////////////////////

    private void Start()
    {
        RequestAuthorization();

#if UNITY_ANDROID
        RegisterNotificationChannel();
#endif

        foreach (var item in notifications)
        {
            if (item != null && item.enabled)
            {
                SendNotification(item.title, item.message, item.fireTimeInHours);
            }
        }
    }

    ///////////////////////////////////// REQUEST PERMISSION

    public void RequestAuthorization()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS"))
        {
            Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS");
        }
#elif UNITY_IOS
        StartCoroutine(RequestAuthorizationIOS());
#endif
    }

#if UNITY_IOS
    private IEnumerator RequestAuthorizationIOS()
    {
        var authOptions = AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound;
        using (var request = new AuthorizationRequest(authOptions, true))
        {
            while (!request.IsFinished)
            {
                yield return null;
            }
        }
    }
#endif

    /////////////////////////////////////

#if UNITY_ANDROID

    public void RegisterNotificationChannel()
    {
        var channel = new AndroidNotificationChannel
        {
            Id = "default_channel",
            Name = "Default Channel",
            Importance = Importance.High,
            Description = "Generic notifications"
        };

        AndroidNotificationCenter.RegisterNotificationChannel(channel);
    }

#endif

    // Notes : parameter fireTimeInHours is in hours. 
    // You can use decimal values for minutes (e.g., 0.5f = 30 minutes) to schedule notifications.

    public void SendNotification(string title, string text, float fireTimeInHours)
    {
#if UNITY_ANDROID

        var androidNotification = new AndroidNotification();
        androidNotification.Title = title;
        androidNotification.Text = text;
        androidNotification.FireTime = DateTime.Now.AddHours(fireTimeInHours);
        androidNotification.SmallIcon = "icon_1";
        androidNotification.LargeIcon = "icon_0";

        AndroidNotificationCenter.SendNotification(androidNotification, "default_channel");

#elif UNITY_IOS

        var timeTrigger = new iOSNotificationTimeIntervalTrigger
        {
            TimeInterval = TimeSpan.FromHours(fireTimeInHours),
            Repeats = false
        };

        var iosNotification = new iOSNotification
        {
            Identifier = Guid.NewGuid().ToString(),
            Title = title,
            Body = text,
            ShowInForeground = true,
            ForegroundPresentationOption = PresentationOption.Alert | PresentationOption.Sound,
            Trigger = timeTrigger
        };

        iOSNotificationCenter.ScheduleNotification(iosNotification);

#endif
    }

    // If you want to cancel all notifications, you can call this method.

    public void CancelAllNotifications()
    {
#if UNITY_ANDROID
        AndroidNotificationCenter.CancelAllNotifications();
#elif UNITY_IOS
        iOSNotificationCenter.RemoveAllScheduledNotifications();
        iOSNotificationCenter.RemoveAllDeliveredNotifications();
#endif
    }
}