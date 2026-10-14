using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using WolfTeen.Friends;

/// <summary>One row of the friend list; spawned by FriendListView, which calls Setup().</summary>
public class FriendItemUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI txtUsername;
    [SerializeField] private Image           imgOnlineIndicator;
    [SerializeField] private Button          btnRemove;
    [SerializeField] private Button          btnInviteGame;

    [Header("Sprites")]
    [FormerlySerializedAs("onlineColor")]
    [SerializeField] private Sprite onlineSprite;
    [FormerlySerializedAs("offlineColor")]
    [SerializeField] private Sprite offlineSprite;

    private FriendEntry _entry;

    public void Setup(FriendEntry entry)
    {
        _entry = entry;

        txtUsername.text         = entry.Username;
        imgOnlineIndicator.sprite = entry.IsOnline ? onlineSprite : offlineSprite;

        btnRemove.onClick.RemoveAllListeners();
        btnRemove.onClick.AddListener(OnRemoveClicked);

        if (btnInviteGame != null)
        {
            btnInviteGame.onClick.RemoveAllListeners();
            btnInviteGame.onClick.AddListener(OnInviteClicked);
        }
    }

    // Lets presence refreshes update the row without rebuilding the whole list
    public void UpdatePresence(bool isOnline)
    {
        imgOnlineIndicator.sprite = isOnline ? onlineSprite : offlineSprite;
    }

    public void SetInviteButtonVisible(bool visible)
    {
        if (btnInviteGame != null)
            btnInviteGame.gameObject.SetActive(visible);
    }

    private async void OnRemoveClicked()
    {
        btnRemove.interactable = false;
        await FriendRequestManager.Instance.RemoveFriendAsync(_entry.Uid);
        // FriendManager's Firestore listener rebuilds the list, so this row is not destroyed here
        btnRemove.interactable = true;
    }

    private async void OnInviteClicked()
    {
        if (btnInviteGame != null) btnInviteGame.interactable = false;

        bool sent = await GameInviteManager.Instance.SendInviteAsync(_entry);

        if (btnInviteGame != null) btnInviteGame.interactable = true;

        if (!sent)
            Debug.LogWarning($"[FriendItemUI] Invite to '{_entry.Username}' failed (not in room or auth error).");
    }
}
