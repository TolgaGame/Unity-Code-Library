using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;
using WolfTeen.Friends;

/// <summary>
/// Handles sending, accepting, rejecting and cancelling friend requests and removing friends.
/// Firestore paths: users/{uid}, friends/{uid}/list/{friendUid},
/// friendRequests/{uid}/incoming/{fromUid}, friendRequests/{uid}/outgoing/{toUid}.
/// </summary>
public class FriendRequestManager : PersistentSingleton<FriendRequestManager>
{
    public IReadOnlyList<FriendRequestEntry> IncomingRequests => _incoming.AsReadOnly();
    public IReadOnlyList<FriendRequestEntry> OutgoingRequests => _outgoing.AsReadOnly();

    /// <summary>Fires whenever the incoming request list changes.</summary>
    public static event Action<IReadOnlyList<FriendRequestEntry>> OnIncomingRequestsUpdated;

    /// <summary>Fires whenever the outgoing request list changes.</summary>
    public static event Action<IReadOnlyList<FriendRequestEntry>> OnOutgoingRequestsUpdated;

    private readonly List<FriendRequestEntry> _incoming = new List<FriendRequestEntry>();
    private readonly List<FriendRequestEntry> _outgoing = new List<FriendRequestEntry>();

    private ListenerRegistration _incomingListener;
    private ListenerRegistration _outgoingListener;

    private UserProfile          _localUser;
    private FirebaseFirestore    _db;

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
        DetachListeners();
    }

    private void HandleSignIn(UserProfile profile)
    {
        _localUser = profile;
        _db        = FirebaseFirestore.DefaultInstance;
        AttachListeners();
    }

    private void HandleSignOut()
    {
        _localUser = null;
        DetachListeners();
        _incoming.Clear();
        _outgoing.Clear();
        OnIncomingRequestsUpdated?.Invoke(_incoming.AsReadOnly());
        OnOutgoingRequestsUpdated?.Invoke(_outgoing.AsReadOnly());
    }

    private void AttachListeners()
    {
        if (_localUser == null) return;

        _incomingListener = _db
            .Collection("friendRequests")
            .Document(_localUser.Uid)
            .Collection("incoming")
            .Listen(snap =>
            {
                _incoming.Clear();
                foreach (var doc in snap.Documents)
                {
                    if (!doc.Exists) continue;
                    _incoming.Add(DocToRequest(doc));
                }
                OnIncomingRequestsUpdated?.Invoke(_incoming.AsReadOnly());
            });

        _outgoingListener = _db
            .Collection("friendRequests")
            .Document(_localUser.Uid)
            .Collection("outgoing")
            .Listen(snap =>
            {
                _outgoing.Clear();
                foreach (var doc in snap.Documents)
                {
                    if (!doc.Exists) continue;
                    _outgoing.Add(DocToRequest(doc));
                }
                OnOutgoingRequestsUpdated?.Invoke(_outgoing.AsReadOnly());
            });
    }

    private void DetachListeners()
    {
        _incomingListener?.Stop();
        _outgoingListener?.Stop();
        _incomingListener = null;
        _outgoingListener = null;
    }

    private static FriendRequestEntry DocToRequest(DocumentSnapshot doc)
    {
        return new FriendRequestEntry
        {
            Uid          = doc.Id,
            Username     = doc.ContainsField("username")     ? doc.GetValue<string>("username")     : doc.Id,
            PhotonUserId = doc.ContainsField("photonUserId") ? doc.GetValue<string>("photonUserId") : doc.Id,
            SentAt       = doc.ContainsField("sentAt")
                               ? doc.GetValue<Timestamp>("sentAt").ToDateTime()
                               : DateTime.UtcNow
        };
    }

    /// <summary>
    /// Sends a friend request to the user with the given username.
    /// Returns a result code so the UI can show the appropriate message.
    /// </summary>
    public async Task<FriendRequestResult> SendFriendRequestAsync(string targetUsername)
    {
        if (_localUser == null)
            return FriendRequestResult.FirestoreError;

        string sanitized = AuthManager.SanitizeUsername(targetUsername);

        if (string.Equals(sanitized, _localUser.Username, StringComparison.OrdinalIgnoreCase))
            return FriendRequestResult.CannotAddSelf;

        QuerySnapshot query = await _db
            .Collection("users")
            .WhereEqualTo("username", sanitized)
            .Limit(1)
            .GetSnapshotAsync();

        if (query.Count == 0)
            return FriendRequestResult.UserNotFound;

        DocumentSnapshot targetDoc = query.Documents.First();
        string targetUid           = targetDoc.Id;
        string targetPhotonId      = targetDoc.ContainsField("photonUserId")
                                         ? targetDoc.GetValue<string>("photonUserId")
                                         : sanitized;

        DocumentSnapshot existingFriend = await _db
            .Collection("friends")
            .Document(_localUser.Uid)
            .Collection("list")
            .Document(targetUid)
            .GetSnapshotAsync();

        if (existingFriend.Exists)
            return FriendRequestResult.AlreadyFriends;

        DocumentSnapshot existingOutgoing = await _db
            .Collection("friendRequests")
            .Document(_localUser.Uid)
            .Collection("outgoing")
            .Document(targetUid)
            .GetSnapshotAsync();

        if (existingOutgoing.Exists)
            return FriendRequestResult.RequestAlreadySent;

        // The target already asked us: the user should accept that request instead
        DocumentSnapshot existingIncoming = await _db
            .Collection("friendRequests")
            .Document(_localUser.Uid)
            .Collection("incoming")
            .Document(targetUid)
            .GetSnapshotAsync();

        if (existingIncoming.Exists)
            return FriendRequestResult.IncomingRequestExists;

        Timestamp now = Timestamp.GetCurrentTimestamp();

        // Stored in the target's incoming list, so it describes us
        var incomingRequestData = new Dictionary<string, object>
        {
            { "username",     _localUser.Username     },
            { "photonUserId", _localUser.PhotonUserId },
            { "sentAt",       now                     }
        };

        // Stored in our outgoing list, so it describes the target
        var outgoingRequestData = new Dictionary<string, object>
        {
            { "username",     sanitized    },
            { "photonUserId", targetPhotonId },
            { "sentAt",       now           }
        };

        WriteBatch batch = _db.StartBatch();

        batch.Set(
            _db.Collection("friendRequests").Document(targetUid).Collection("incoming").Document(_localUser.Uid),
            incomingRequestData);

        batch.Set(
            _db.Collection("friendRequests").Document(_localUser.Uid).Collection("outgoing").Document(targetUid),
            outgoingRequestData);

        try
        {
            await batch.CommitAsync();
            Debug.Log($"[FriendRequestManager] Request sent to '{sanitized}'.");
            return FriendRequestResult.Success;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FriendRequestManager] SendFriendRequest failed: {ex.Message}");
            return FriendRequestResult.FirestoreError;
        }
    }

    /// <summary>
    /// Accepts an incoming friend request from <paramref name="fromUid"/>.
    /// Atomically: adds friend entries on both sides, removes both request documents.
    /// </summary>
    public async Task<bool> AcceptRequestAsync(string fromUid)
    {
        if (_localUser == null) return false;

        // The sender's username and Photon ID come from the incoming request document
        DocumentSnapshot incomingDoc = await _db
            .Collection("friendRequests")
            .Document(_localUser.Uid)
            .Collection("incoming")
            .Document(fromUid)
            .GetSnapshotAsync();

        if (!incomingDoc.Exists)
        {
            Debug.LogWarning($"[FriendRequestManager] No incoming request from {fromUid}.");
            return false;
        }

        string fromUsername     = incomingDoc.ContainsField("username")     ? incomingDoc.GetValue<string>("username")     : fromUid;
        string fromPhotonUserId = incomingDoc.ContainsField("photonUserId") ? incomingDoc.GetValue<string>("photonUserId") : fromUid;

        Timestamp now = Timestamp.GetCurrentTimestamp();

        var myFriendEntry = new Dictionary<string, object>
        {
            { "username",     fromUsername     },
            { "photonUserId", fromPhotonUserId },
            { "addedAt",      now              }
        };

        var theirFriendEntry = new Dictionary<string, object>
        {
            { "username",     _localUser.Username     },
            { "photonUserId", _localUser.PhotonUserId },
            { "addedAt",      now                     }
        };

        WriteBatch batch = _db.StartBatch();

        batch.Set(
            _db.Collection("friends").Document(_localUser.Uid).Collection("list").Document(fromUid),
            myFriendEntry);

        batch.Set(
            _db.Collection("friends").Document(fromUid).Collection("list").Document(_localUser.Uid),
            theirFriendEntry);

        batch.Delete(
            _db.Collection("friendRequests").Document(_localUser.Uid).Collection("incoming").Document(fromUid));

        batch.Delete(
            _db.Collection("friendRequests").Document(fromUid).Collection("outgoing").Document(_localUser.Uid));

        try
        {
            await batch.CommitAsync();
            Debug.Log($"[FriendRequestManager] Accepted request from '{fromUsername}'.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FriendRequestManager] AcceptRequest failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Rejects an incoming friend request from <paramref name="fromUid"/>.
    /// Removes the request on both sides without adding a friend entry.
    /// </summary>
    public async Task<bool> RejectRequestAsync(string fromUid)
    {
        if (_localUser == null) return false;

        WriteBatch batch = _db.StartBatch();

        batch.Delete(
            _db.Collection("friendRequests").Document(_localUser.Uid).Collection("incoming").Document(fromUid));

        batch.Delete(
            _db.Collection("friendRequests").Document(fromUid).Collection("outgoing").Document(_localUser.Uid));

        try
        {
            await batch.CommitAsync();
            Debug.Log($"[FriendRequestManager] Rejected request from '{fromUid}'.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FriendRequestManager] RejectRequest failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Cancels a pending outgoing friend request to <paramref name="toUid"/>.
    /// </summary>
    public async Task<bool> CancelOutgoingRequestAsync(string toUid)
    {
        if (_localUser == null) return false;

        WriteBatch batch = _db.StartBatch();

        batch.Delete(
            _db.Collection("friendRequests").Document(_localUser.Uid).Collection("outgoing").Document(toUid));

        batch.Delete(
            _db.Collection("friendRequests").Document(toUid).Collection("incoming").Document(_localUser.Uid));

        try
        {
            await batch.CommitAsync();
            Debug.Log($"[FriendRequestManager] Cancelled request to '{toUid}'.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FriendRequestManager] CancelOutgoingRequest failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Removes a friend from both sides' friend lists.
    /// </summary>
    public async Task<bool> RemoveFriendAsync(string friendUid)
    {
        if (_localUser == null) return false;

        WriteBatch batch = _db.StartBatch();

        batch.Delete(
            _db.Collection("friends").Document(_localUser.Uid).Collection("list").Document(friendUid));

        batch.Delete(
            _db.Collection("friends").Document(friendUid).Collection("list").Document(_localUser.Uid));

        try
        {
            await batch.CommitAsync();
            Debug.Log($"[FriendRequestManager] Removed friend '{friendUid}'.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FriendRequestManager] RemoveFriend failed: {ex.Message}");
            return false;
        }
    }
}
