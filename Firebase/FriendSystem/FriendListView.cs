// ─────────────────────────────────────────────────────────────────────────────
// FriendListView.cs
// Attach to the FriendPanel (or the ScrollView root inside it).
// Subscribes to FriendManager.OnFriendsUpdated and rebuilds the list.
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections.Generic;
using UnityEngine;
using WolfTeen.Friends;

public class FriendListView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform       content;        // ScrollView → Viewport → Content
    [SerializeField] private FriendItemUI    friendItemPrefab;
    [SerializeField] private GameObject      emptyStateLabel; // "Henüz arkadaşın yok" yazısı (opsiyonel)

    private readonly List<FriendItemUI> _spawnedItems = new List<FriendItemUI>();
    private bool _showInviteButton = false;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        FriendManager.OnFriendsUpdated += RefreshList;

        // Panel açılınca mevcut listeyi hemen yükle.
        if (FriendManager.Instance != null)
            RefreshList(FriendManager.Instance.Friends);
    }

    private void OnDisable()
    {
        FriendManager.OnFriendsUpdated -= RefreshList;
    }

    // ── List rebuild ──────────────────────────────────────────────────────────

    private void RefreshList(IReadOnlyList<FriendEntry> friends)
    {
        // Temizle
        foreach (var item in _spawnedItems)
        {
            if (item != null) Destroy(item.gameObject);
        }
        _spawnedItems.Clear();

        // Boş durum
        if (emptyStateLabel != null)
            emptyStateLabel.SetActive(friends.Count == 0);

        // Yeniden oluştur
        foreach (var entry in friends)
        {
            FriendItemUI item = Instantiate(friendItemPrefab, content);
            item.Setup(entry);
            item.SetInviteButtonVisible(_showInviteButton);
            item.gameObject.SetActive(true);
            _spawnedItems.Add(item);
        }
    }

    public void SetInviteButtonsVisible(bool visible)
    {
        _showInviteButton = visible;
        foreach (var item in _spawnedItems)
        {
            if (item != null)
                item.SetInviteButtonVisible(visible);
        }
    }
}
