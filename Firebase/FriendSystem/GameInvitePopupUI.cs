using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WolfTeen.Friends;

/// <summary>Popup that shows incoming game invites one at a time; extra invites are queued.</summary>
public class GameInvitePopupUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject      popupRoot;
    [SerializeField] private TextMeshProUGUI txtMessage;
    [SerializeField] private Button          btnAccept;
    [SerializeField] private Button          btnReject;

    private readonly Queue<GameInviteData> _queue   = new Queue<GameInviteData>();
    private GameInviteData                 _current;
    private bool                           _showing;
    private Coroutine                      _autoRejectCoroutine;

    // The invite is rejected automatically if the user does not respond in time
    private const float AutoRejectSeconds = 600f; // 10 minutes

    private void Start()
    {
        btnAccept.onClick.AddListener(OnAcceptClicked);
        btnReject.onClick.AddListener(OnRejectClicked);
        popupRoot.SetActive(false);
    }

    private void OnEnable()
    {
        GameInviteManager.OnInviteReceived += EnqueueInvite;
    }

    private void OnDisable()
    {
        GameInviteManager.OnInviteReceived -= EnqueueInvite;
    }

    private void EnqueueInvite(GameInviteData invite)
    {
        Debug.Log($"[GameInvitePopupUI] Invite received. Sender: {invite.InviterUsername}, Room: {invite.RoomName}");
        _queue.Enqueue(invite);
        if (!_showing) ShowNext();
    }

    private void ShowNext()
    {
        if (_autoRejectCoroutine != null)
        {
            StopCoroutine(_autoRejectCoroutine);
            _autoRejectCoroutine = null;
        }

        if (_queue.Count == 0)
        {
            _showing = false;
            popupRoot.SetActive(false);
            return;
        }

        _current = _queue.Dequeue();
        _showing  = true;

        txtMessage.text = $"{_current.InviterUsername} invited you to their room!";
        SetButtonsInteractable(true);
        popupRoot.SetActive(true);

        _autoRejectCoroutine = StartCoroutine(AutoRejectAfterDelay());
    }

    private IEnumerator AutoRejectAfterDelay()
    {
        yield return new WaitForSeconds(AutoRejectSeconds);
        Debug.Log($"[GameInvitePopupUI] Auto-rejecting stale invite from '{_current?.InviterUsername}'.");
        SetButtonsInteractable(false);
        if (_current != null)
            _ = GameInviteManager.Instance.RejectInviteAsync(_current);
        ShowNext();
    }

    private async void OnAcceptClicked()
    {
        if (_autoRejectCoroutine != null) { StopCoroutine(_autoRejectCoroutine); _autoRejectCoroutine = null; }
        SetButtonsInteractable(false);
        await GameInviteManager.Instance.AcceptInviteAsync(_current);
        ShowNext();
    }

    private async void OnRejectClicked()
    {
        if (_autoRejectCoroutine != null) { StopCoroutine(_autoRejectCoroutine); _autoRejectCoroutine = null; }
        SetButtonsInteractable(false);
        await GameInviteManager.Instance.RejectInviteAsync(_current);
        ShowNext();
    }

    private void SetButtonsInteractable(bool value)
    {
        btnAccept.interactable = value;
        btnReject.interactable = value;
    }
}
