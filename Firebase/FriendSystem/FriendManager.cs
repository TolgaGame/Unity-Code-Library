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

/// <summary>
/// Keeps the local friend list in sync. Firestore is the source of truth for who the friends are;
/// Photon FindFriends (polled, rate-limited by Photon to roughly 5 s) tells who is online.
/// </summary>
public class FriendManager : PersistentSingleton<FriendManager>, IMatchmakingCallbacks
{
    [Tooltip("How often (in seconds) to poll Photon for friend presence. Min ~5 s.")]
    [SerializeField] private float presencePollInterval = 30f;

    /// <summary>The current friend list. Presence flags are updated on each poll.</summary>
    public IReadOnlyList<FriendEntry> Friends => _friends.AsReadOnly();

    /// <summary>
    /// Fired whenever the friend list changes (load, add, remove)
    /// or when presence data is refreshed from Photon.
    /// Always invoked on the main thread.
    /// </summary>
    public static event Action<IReadOnlyList<FriendEntry>> OnFriendsUpdated;

    private readonly List<FriendEntry> _friends     = new List<FriendEntry>();
    private ListenerRegistration       _listenerReg;
    private Coroutine                  _pollCoroutine;
    private string                     _localUid;
    private bool                       _photonReady;

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
            // The Firebase Unity SDK runs this callback on the main thread
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

        // Notify the UI right away with whatever presence data we already have
        OnFriendsUpdated?.Invoke(_friends.AsReadOnly());

        if (_photonReady)
            RequestPresenceRefresh();
    }

    private void HandlePhotonConnected()
    {
        _photonReady = true;

        Debug.Log("[FriendManager] Friend system ready: Firebase authenticated, Photon connected, presence polling active.");

        if (_friends.Count > 0)
            StartPresencePolling();
    }

    private void HandlePhotonDisconnected(DisconnectCause cause)
    {
        _photonReady = false;
        StopPresencePolling();

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
        // Let Photon settle after connecting before the first poll
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

        // FindFriends is dropped while in a room (PUN 2); the last known presence stays valid
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

    public void OnFriendListUpdate(List<FriendInfo> photonFriendList)
    {
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

    // Required by IMatchmakingCallbacks but unused
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
