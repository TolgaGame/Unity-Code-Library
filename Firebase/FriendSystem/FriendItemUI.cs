// ─────────────────────────────────────────────────────────────────────────────
// FriendItemUI.cs
// Attach to the FriendItem prefab (one row in the friend list).
// FriendListView spawns this and calls Setup().
// ─────────────────────────────────────────────────────────────────────────────

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WolfTeen.Friends;

public class FriendItemUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI txtUsername;
    [SerializeField] private Image           imgOnlineIndicator;
    [SerializeField] private Button          btnRemove;
    [SerializeField] private Button          btnInviteGame;

    [Header("Colors")]
    [SerializeField] private Sprite onlineColor;
    [SerializeField] private Sprite offlineColor;


    private FriendEntry _entry;

    public void Setup(FriendEntry entry)
    {
        _entry = entry;

        txtUsername.text         = entry.Username;
        imgOnlineIndicator.sprite = entry.IsOnline ? onlineColor : offlineColor;

        btnRemove.onClick.RemoveAllListeners();
        btnRemove.onClick.AddListener(OnRemoveClicked);

        if (btnInviteGame != null)
        {
            btnInviteGame.onClick.RemoveAllListeners();
            btnInviteGame.onClick.AddListener(OnInviteClicked);
        }
    }

    // Called when presence data is refreshed — no need to rebuild the whole list.
    public void UpdatePresence(bool isOnline)
    {
        imgOnlineIndicator.sprite = isOnline ? onlineColor : offlineColor;
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
        // FriendManager's Firestore listener will rebuild the list automatically.
        // No need to destroy this object manually.
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
