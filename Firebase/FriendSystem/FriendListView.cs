using System.Collections.Generic;
using UnityEngine;
using WolfTeen.Friends;

/// <summary>Rebuilds the friend list UI whenever FriendManager.OnFriendsUpdated fires.</summary>
public class FriendListView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform       content;        // ScrollView > Viewport > Content
    [SerializeField] private FriendItemUI    friendItemPrefab;
    [SerializeField] private GameObject      emptyStateLabel; // Optional "no friends yet" label

    private readonly List<FriendItemUI> _spawnedItems = new List<FriendItemUI>();
    private bool _showInviteButton = false;

    private void OnEnable()
    {
        FriendManager.OnFriendsUpdated += RefreshList;

        // The panel may open after the list was already loaded
        if (FriendManager.Instance != null)
            RefreshList(FriendManager.Instance.Friends);
    }

    private void OnDisable()
    {
        FriendManager.OnFriendsUpdated -= RefreshList;
    }

    private void RefreshList(IReadOnlyList<FriendEntry> friends)
    {
        foreach (var item in _spawnedItems)
        {
            if (item != null) Destroy(item.gameObject);
        }
        _spawnedItems.Clear();

        if (emptyStateLabel != null)
            emptyStateLabel.SetActive(friends.Count == 0);

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
