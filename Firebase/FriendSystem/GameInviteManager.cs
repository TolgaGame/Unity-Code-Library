// ─────────────────────────────────────────────────────────────────────────────
// GameInviteManager.cs
// Responsibilities:
//   1. Send a game invite to a friend (inviter must be in a Photon room).
//   2. Listen to incoming invites via Firestore real-time listener.
//   3. Accept (delete doc + PhotonNetwork.JoinRoom) or Reject (delete doc).
//
// Firestore path: gameInvites/{targetUid}/pending/{inviterUid}
//   Fields: inviterUsername (string), roomName (string), sentAt (timestamp)
//
// Communication channel: Firestore (cross-room — Photon RaiseEvent only works
// within the same room, so Firestore is required for lobby↔room invites).
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using Photon.Pun;
using UnityEngine;
using WolfTeen.Friends;

public class GameInviteManager : PersistentSingleton<GameInviteManager>
{
    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>Fired on the main thread whenever a new game invite arrives.</summary>
    public static event Action<GameInviteData> OnInviteReceived;

    // ── Internals ─────────────────────────────────────────────────────────────

    private UserProfile          _localUser;
    private FirebaseFirestore    _db;
    private ListenerRegistration _listener;

    // Invites older than this are silently ignored (stale from previous sessions).
    private static readonly TimeSpan InviteExpiry = TimeSpan.FromMinutes(5);

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        AuthManager.OnSignInComplete += HandleSignIn;
        AuthManager.OnSignedOut      += HandleSignOut;
    }

    private void OnDisable()
    {
        AuthManager.OnSignInComplete -= HandleSignIn;
        AuthManager.OnSignedOut      -= HandleSignOut;
    }

    private void OnDestroy()
    {
        _listener?.Stop();
        _listener = null;
    }

    // ── Auth handlers ─────────────────────────────────────────────────────────

    private void HandleSignIn(UserProfile profile)
    {
        _localUser = profile;
        _db        = FirebaseFirestore.DefaultInstance;
        AttachListener();
    }

    private void HandleSignOut()
    {
        _localUser = null;
        _listener?.Stop();
        _listener  = null;
    }

    // ── Firestore real-time listener ──────────────────────────────────────────

    private void AttachListener()
    {
        if (_localUser == null) return;

        _listener?.Stop();

        _listener = _db
            .Collection("gameInvites")
            .Document(_localUser.Username)
            .Collection("pending")
            .Listen(snap =>
            {
                // Firebase Unity SDK guarantees main-thread delivery.
                foreach (var change in snap.GetChanges(MetadataChanges.Exclude))
                {
                    if (change.ChangeType != DocumentChange.Type.Added) continue;
                    if (!change.Document.Exists) continue;

                    GameInviteData invite = DocToInvite(change.Document);
                    if (invite == null)
                    {
                        // Stale or malformed — delete from Firestore so it never reappears.
                        DeleteInviteDocAsync(change.Document.Id);
                        continue;
                    }

                    OnInviteReceived?.Invoke(invite);
                }
            });
    }

    private static GameInviteData DocToInvite(DocumentSnapshot doc)
    {
        // Discard invites older than InviteExpiry to avoid showing stale popups.
        if (doc.ContainsField("sentAt"))
        {
            DateTime sentAt = doc.GetValue<Timestamp>("sentAt").ToDateTime();
            if (DateTime.UtcNow - sentAt > InviteExpiry)
                return null;
        }

        string roomName = doc.ContainsField("roomName") ? doc.GetValue<string>("roomName") : string.Empty;
        if (string.IsNullOrEmpty(roomName))
            return null;

        return new GameInviteData
        {
            InviterUid      = doc.Id,
            InviterUsername = doc.ContainsField("inviterUsername")
                                  ? doc.GetValue<string>("inviterUsername")
                                  : doc.Id,
            RoomName        = roomName
        };
    }

    // ── Send invite ───────────────────────────────────────────────────────────

    /// <summary>
    /// Writes an invite document to Firestore for the target friend.
    /// The local player must currently be in a Photon room.
    /// </summary>
    public async Task<bool> SendInviteAsync(FriendEntry friend)
    {
        if (_localUser == null || _db == null)
        {
            Debug.LogWarning("[GameInviteManager] Not authenticated.");
            return false;
        }

        if (!PhotonNetwork.InRoom)
        {
            Debug.LogWarning("[GameInviteManager] Local player is not in a room.");
            return false;
        }

        string roomName = PhotonNetwork.CurrentRoom.Name;

        var data = new Dictionary<string, object>
        {
            { "inviterUsername", _localUser.Username             },
            { "roomName",        roomName                        },
            { "sentAt",          Timestamp.GetCurrentTimestamp() }
        };

        try
        {
            await _db
                .Collection("gameInvites")
                .Document(friend.Username)
                .Collection("pending")
                .Document(_localUser.Username)
                .SetAsync(data);

            Debug.Log($"[GameInviteManager] Invite sent to '{friend.Username}' for room '{roomName}'.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameInviteManager] SendInvite failed: {ex.Message}");
            return false;
        }
    }

    // ── Accept ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Deletes the invite document and joins the Photon room.
    /// </summary>
    public async Task AcceptInviteAsync(GameInviteData invite)
    {
        if (_localUser == null || _db == null) return;

        await DeleteInviteDocAsync(invite.InviterUid);

        if (PhotonNetwork.InRoom)
        {
            Debug.LogWarning("[GameInviteManager] Already in a room, cannot accept invite.");
            return;
        }

        if (string.IsNullOrEmpty(invite.RoomName))
        {
            Debug.LogWarning("[GameInviteManager] Invite has no room name.");
            return;
        }

        // Fade screen before joining (mirrors what bl_Lobby does for normal room joins).
        if (bl_LobbyUI.Instance != null)
            bl_LobbyUI.Instance.blackScreenFader.FadeIn(0.3f);

        PhotonNetwork.JoinRoom(invite.RoomName);
        Debug.Log($"[GameInviteManager] Joining room '{invite.RoomName}' via invite from '{invite.InviterUsername}'.");
    }

    // ── Reject ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Deletes the invite document without joining the room.
    /// </summary>
    public async Task RejectInviteAsync(GameInviteData invite)
    {
        if (_localUser == null || _db == null) return;

        await DeleteInviteDocAsync(invite.InviterUid);
        Debug.Log($"[GameInviteManager] Rejected invite from '{invite.InviterUsername}'.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task DeleteInviteDocAsync(string inviterUid)
    {
        try
        {
            await _db
                .Collection("gameInvites")
                .Document(_localUser.Username)
                .Collection("pending")
                .Document(inviterUid)
                .DeleteAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameInviteManager] Delete invite doc failed: {ex.Message}");
        }
    }
}
