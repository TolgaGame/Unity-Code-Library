// ─────────────────────────────────────────────────────────────────────────────
// IncomingRequestItemUI.cs
// Attach to the IncomingRequestItem prefab.
// One row per incoming friend request.
// ─────────────────────────────────────────────────────────────────────────────

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WolfTeen.Friends;

public class IncomingRequestItemUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI txtUsername;
    [SerializeField] private Button          btnAccept;
    [SerializeField] private Button          btnReject;

    private FriendRequestEntry _entry;

    public void Setup(FriendRequestEntry entry)
    {
        _entry           = entry;
        txtUsername.text = entry.Username;

        btnAccept.onClick.RemoveAllListeners();
        btnReject.onClick.RemoveAllListeners();

        btnAccept.onClick.AddListener(OnAcceptClicked);
        btnReject.onClick.AddListener(OnRejectClicked);
    }

    private async void OnAcceptClicked()
    {
        SetButtonsInteractable(false);
        await FriendRequestManager.Instance.AcceptRequestAsync(_entry.Uid);
        // Firestore listener otomatik listeyi günceller, bu obje zaten destroy edilir.
    }

    private async void OnRejectClicked()
    {
        SetButtonsInteractable(false);
        await FriendRequestManager.Instance.RejectRequestAsync(_entry.Uid);
    }

    private void SetButtonsInteractable(bool value)
    {
        btnAccept.interactable = value;
        btnReject.interactable = value;
    }
}
