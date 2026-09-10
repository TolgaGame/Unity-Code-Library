// ─────────────────────────────────────────────────────────────────────────────
// IncomingRequestsView.cs
// Attach to the incoming requests panel (ScrollView root).
// Also drives the notification badge on the friend button.
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using WolfTeen.Friends;

public class IncomingRequestsView : MonoBehaviour
{
    [Header("List")]
    [SerializeField] private Transform              content;
    [SerializeField] private IncomingRequestItemUI  itemPrefab;
    [SerializeField] private GameObject             emptyStateLabel;

    [Header("Notification Badge")]
    [SerializeField] private GameObject      badgeObject;   // badge root (aktif/pasif)
    [SerializeField] private TextMeshProUGUI badgeCount;    // içindeki sayı

    private readonly List<IncomingRequestItemUI> _spawnedItems = new List<IncomingRequestItemUI>();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        FriendRequestManager.OnIncomingRequestsUpdated += RefreshList;

        if (FriendRequestManager.Instance != null)
            RefreshList(FriendRequestManager.Instance.IncomingRequests);
    }

    private void OnDisable()
    {
        FriendRequestManager.OnIncomingRequestsUpdated -= RefreshList;
    }

    // ── List rebuild ──────────────────────────────────────────────────────────

    private void RefreshList(IReadOnlyList<FriendRequestEntry> requests)
    {
        foreach (var item in _spawnedItems)
            if (item != null) Destroy(item.gameObject);
        _spawnedItems.Clear();

        if (emptyStateLabel != null)
            emptyStateLabel.SetActive(requests.Count == 0);

        foreach (var entry in requests)
        {
            var item = Instantiate(itemPrefab, content);
            item.gameObject.SetActive(true);
            item.Setup(entry);
            _spawnedItems.Add(item);
        }

        UpdateBadge(requests.Count);
    }

    private void UpdateBadge(int count)
    {
        if (badgeObject == null) return;
        badgeObject.SetActive(count > 0);
        if (badgeCount != null)
            badgeCount.text = count > 9 ? "9+" : count.ToString();
    }
}
