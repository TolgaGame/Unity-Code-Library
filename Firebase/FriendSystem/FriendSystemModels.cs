// ─────────────────────────────────────────────────────────────────────────────
// FriendSystemModels.cs
// Shared data models for the Friend System.
// No Unity or Firebase dependencies — pure C# data containers.
// ─────────────────────────────────────────────────────────────────────────────

using System;

namespace WolfTeen.Friends
{
    // ── Firestore path: users/{uid} ─────────────────────────────────────────
    [Serializable]
    public class UserProfile
    {
        public string Uid          { get; set; }
        public string Username     { get; set; }
        public string PhotonUserId { get; set; }  // always == Username
    }

    // ── Firestore path: friends/{uid}/list/{friendUid} ──────────────────────
    [Serializable]
    public class FriendEntry
    {
        public string   Uid          { get; set; }
        public string   Username     { get; set; }
        public string   PhotonUserId { get; set; }
        public DateTime AddedAt      { get; set; }

        // Populated at runtime by FriendManager via Photon FindFriends.
        // Not persisted to Firestore.
        public bool IsOnline  { get; set; }
        public bool IsInRoom  { get; set; }
    }

    // ── Firestore paths:
    //    friendRequests/{uid}/incoming/{fromUid}
    //    friendRequests/{uid}/outgoing/{toUid}   ─────────────────────────────
    [Serializable]
    public class FriendRequestEntry
    {
        public string   Uid          { get; set; }
        public string   Username     { get; set; }
        public string   PhotonUserId { get; set; }
        public DateTime SentAt       { get; set; }
    }

    // ── Result codes returned by FriendRequestManager operations ─────────────
    public enum FriendRequestResult
    {
        Success,
        UserNotFound,
        AlreadyFriends,
        RequestAlreadySent,
        IncomingRequestExists,  // they already sent you a request — just accept
        CannotAddSelf,
        FirestoreError
    }

    // ── Firestore path: gameInvites/{targetUsername}/pending/{inviterUsername} ──
    [Serializable]
    public class GameInviteData
    {
        public string InviterUid      { get; set; }
        public string InviterUsername { get; set; }
        public string RoomName        { get; set; }
    }
}
