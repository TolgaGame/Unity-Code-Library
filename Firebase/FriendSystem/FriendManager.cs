// ─────────────────────────────────────────────────────────────────────────────
// FriendManager.cs
// Responsibilities:
//   1. Load and maintain the local player's friend list from Firestore.
//   2. Use Photon FindFriends to determine which friends are online.
//   3. Expose events so UI can reactively render the friend list.
//
// Design notes:
//   - Firestore is the source of truth for WHO your friends are.
//   - Photon is the source of truth for WHETHER they are online right now.
//   - FindFriends is polled on a configurable interval (default 30 s).
//     Photon enforces its own rate limit on this call (~5 s minimum).
//   - The Firestore listener is real-time: adding/removing a friend is
//     reflected immediately without needing a manual refresh.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebase.Extensions;
using Firebase.Firestore;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using WolfTeen.Friends;

public class FriendManager : PersistentSingleton<FriendManager>, IMatchmakingCallbacks
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Tooltip("How often (in seconds) to poll Photon for friend presence. Min ~5 s.")]
    [SerializeField] private float presencePollInterval = 30f;

    // ── Public state ──────────────────────────────────────────────────────────

    /// <summary>The current friend list. Presence flags are updated on each poll.</summary>
    public IReadOnlyList<FriendEntry> Friends => _friends.AsReadOnly();

    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fired whenever the friend list changes (load, add, remove)
    /// or when presence data is refreshed from Photon.
    /// Always invoked on the main thread.
    /// </summary>
    public static event Action<IReadOnlyList<FriendEntry>> OnFriendsUpdated;

    // ── Internals ─────────────────────────────────────────────────────────────

    private readonly List<FriendEntry> _friends     = new List<FriendEntry>();
    private ListenerRegistration       _listenerReg;
    private Coroutine                  _pollCoroutine;
    private string                     _localUid;
    private bool                       _photonReady;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnEnable()
    {
        AuthManager.OnSignInComplete     += HandleSignIn;
        AuthManager.OnSignedOut          += HandleSignOut;
        AuthManager.OnPhotonConnected    += HandlePhotonConnected;
        AuthManager.OnPhotonDisconnected += HandlePhotonDisconnected;
    }

    private void OnDisable()
    {
        AuthManager.OnSignInComplete     -= HandleSignIn;
        AuthManager.OnSignedOut          -= HandleSignOut;
        AuthManager.OnPhotonConnected    -= HandlePhotonConnected;
        AuthManager.OnPhotonDisconnected -= HandlePhotonDisconnected;
    }

    private void OnDestroy()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
        StopPresencePolling();
        _listenerReg?.Stop();
        _listenerReg = null;
    }

    // ── Auth handlers ─────────────────────────────────────────────────────────

    private void HandleSignIn(UserProfile profile)
    {
        _localUid = profile.Uid;
        AttachFirestoreListener();
    }

    private void HandleSignOut()
    {
        _localUid = null;
        StopPresencePolling();
        _listenerReg?.Stop();
        _listenerReg = null;
        _friends.Clear();
        OnFriendsUpdated?.Invoke(_friends.AsReadOnly());
    }

    // ── Firestore real-time listener ──────────────────────────────────────────

    private void AttachFirestoreListener()
    {
        if (string.IsNullOrEmpty(_localUid)) return;

        _listenerReg?.Stop();

        CollectionReference friendsCol = FirebaseFirestore.DefaultInstance
            .Collection("friends")
            .Document(_localUid)
            .Collection("list");

        _listenerReg = friendsCol.Listen(snapshot =>
        {
            // This callback is guaranteed to run on the main thread by the Firebase Unity SDK.
            RebuildFriendList(snapshot);
        });
    }

    private void RebuildFriendList(QuerySnapshot snapshot)
    {
        _friends.Clear();

        foreach (DocumentSnapshot doc in snapshot.Documents)
        {
            if (!doc.Exists) continue;

            var entry = new FriendEntry
            {
                Uid          = doc.Id,
                Username     = doc.ContainsField("username")     ? doc.GetValue<string>("username")     : doc.Id,
                PhotonUserId = doc.ContainsField("photonUserId") ? doc.GetValue<string>("photonUserId") : doc.Id,
                AddedAt      = doc.ContainsField("addedAt")
                                   ? doc.GetValue<Timestamp>("addedAt").ToDateTime()
                                   : DateTime.UtcNow
            };

            _friends.Add(entry);
        }

        // Immediately notify UI with whatever presence data we already have.
        OnFriendsUpdated?.Invoke(_friends.AsReadOnly());

        // If Photon is ready, do an immediate presence refresh.
        if (_photonReady)
            RequestPresenceRefresh();
    }

    // ── Photon presence polling ───────────────────────────────────────────────

    private void HandlePhotonConnected()
    {
        _photonReady = true;

        Debug.Log("[FriendSystem] ✔ Friend System Ready — Firebase authenticated, Photon connected, presence polling active.");

        if (_friends.Count > 0)
            StartPresencePolling();
    }

    private void HandlePhotonDisconnected(DisconnectCause cause)
    {
        _photonReady = false;
        StopPresencePolling();

        // Mark everyone offline.
        bool changed = false;
        foreach (var f in _friends)
        {
            if (f.IsOnline || f.IsInRoom)
            {
                f.IsOnline = false;
                f.IsInRoom = false;
                changed    = true;
            }
        }

        if (changed)
            OnFriendsUpdated?.Invoke(_friends.AsReadOnly());
    }

    private void StartPresencePolling()
    {
        StopPresencePolling();
        _pollCoroutine = StartCoroutine(PresencePollCoroutine());
    }

    private void StopPresencePolling()
    {
        if (_pollCoroutine != null)
        {
            StopCoroutine(_pollCoroutine);
            _pollCoroutine = null;
        }
    }

    private IEnumerator PresencePollCoroutine()
    {
        // Small initial delay so Photon can fully settle after connecting.
        yield return new WaitForSeconds(2f);

        while (true)
        {
            RequestPresenceRefresh();
            yield return new WaitForSeconds(presencePollInterval);
        }
    }

    /// <summary>
    /// Calls PhotonNetwork.FindFriends with the Photon user IDs of all friends.
    /// Must be called while connected to Master Server.
    /// </summary>
    public void RequestPresenceRefresh()
    {
        if (!_photonReady || _friends.Count == 0) return;

        // FindFriends requires being connected to Master (not inside a room for Photon PUN 2).
        // If the local player is in a room the call is silently dropped, which is acceptable —
        // the last known presence data remains valid.
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("[FriendManager] Skipping FindFriends: currently in a Photon room.");
            return;
        }

        string[] photonIds = _friends
            .Select(f => f.PhotonUserId)
            .Where(id => !string.IsNullOrEmpty(id))
            .ToArray();

        if (photonIds.Length == 0) return;

        bool sent = PhotonNetwork.FindFriends(photonIds);
        if (!sent)
            Debug.LogWarning("[FriendManager] PhotonNetwork.FindFriends returned false (not connected?).");
    }

    // ── IMatchmakingCallbacks (Photon) ─────────────────────────────────────────

    public void OnFriendListUpdate(List<FriendInfo> photonFriendList)
    {
        // Build a lookup by PhotonUserId for O(1) access.
        var presenceMap = new Dictionary<string, FriendInfo>(photonFriendList.Count);
        foreach (var info in photonFriendList)
            presenceMap[info.UserId] = info;

        bool anyChanged = false;
        foreach (var entry in _friends)
        {
            if (!presenceMap.TryGetValue(entry.PhotonUserId, out FriendInfo info))
                continue;

            bool wasOnline = entry.IsOnline;
            bool wasInRoom = entry.IsInRoom;

            entry.IsOnline = info.IsOnline;
            entry.IsInRoom = info.IsInRoom;

            if (wasOnline != entry.IsOnline || wasInRoom != entry.IsInRoom)
                anyChanged = true;
        }

        if (anyChanged)
            OnFriendsUpdated?.Invoke(_friends.AsReadOnly());
    }

    // Unused IMatchmakingCallbacks — required by the interface.
    public void OnJoinedLobby()                                          { }
    public void OnLeftLobby()                                            { }
    public void OnRoomListUpdate(List<RoomInfo> roomList)                { }
    public void OnJoinedRoom()                                           { }
    public void OnJoinRoomFailed(short returnCode, string message)       { }
    public void OnJoinRandomFailed(short returnCode, string message)     { }
    public void OnLeftRoom()                                             { }
    public void OnCreatedRoom()                                          { }
    public void OnCreateRoomFailed(short returnCode, string message)     { }
    public void OnLobbyStatisticsUpdate(List<TypedLobbyInfo> lobbyStatistics) { }
}
