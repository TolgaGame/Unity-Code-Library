// ─────────────────────────────────────────────────────────────────────────────
// GameInvitePopupUI.cs
// Attach to the GameInvitePopup panel (a root GameObject on a persistent Canvas).
//
// Unity Setup:
//   • popupRoot    → the panel GameObject to show/hide
//   • txtMessage   → e.g.  "PlayerX seni odaya davet ediyor!"
//   • btnAccept    → "Kabul Et" button
//   • btnReject    → "Reddet"  button
//
// Multiple invites are queued and shown one at a time.
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WolfTeen.Friends;

public class GameInvitePopupUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject      popupRoot;
    [SerializeField] private TextMeshProUGUI txtMessage;
    [SerializeField] private Button          btnAccept;
    [SerializeField] private Button          btnReject;

    // ── Internals ─────────────────────────────────────────────────────────────

    private readonly Queue<GameInviteData> _queue   = new Queue<GameInviteData>();
    private GameInviteData                 _current;
    private bool                           _showing;
    private Coroutine                      _autoRejectCoroutine;

    // Auto-reject the popup if the user doesn't respond within this time.
    private const float AutoRejectSeconds = 600f; // 10 minutes

    // ── Lifecycle ─────────────────────────────────────────────────────────────

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

    // ── Queue management ──────────────────────────────────────────────────────

    private void EnqueueInvite(GameInviteData invite)
    {
        Debug.Log($"[GameInvitePopupUI] Davet geldi! Gönderen: {invite.InviterUsername}, Oda: {invite.RoomName}");
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

        txtMessage.text = $"{_current.InviterUsername} seni odaya davet ediyor!";
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

    // ── Button handlers ───────────────────────────────────────────────────────

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

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetButtonsInteractable(bool value)
    {
        btnAccept.interactable = value;
        btnReject.interactable = value;
    }
}
