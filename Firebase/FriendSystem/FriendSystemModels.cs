using System;

namespace WolfTeen.Friends
{
    /// <summary>Firestore path: users/{uid}</summary>
    [Serializable]
    public class UserProfile
    {
        public string Uid          { get; set; }
        public string Username     { get; set; }
        public string PhotonUserId { get; set; }  // always equals Username
    }

    /// <summary>Firestore path: friends/{uid}/list/{friendUid}</summary>
    [Serializable]
    public class FriendEntry
    {
        public string   Uid          { get; set; }
        public string   Username     { get; set; }
        public string   PhotonUserId { get; set; }
        public DateTime AddedAt      { get; set; }

        // Filled at runtime from Photon FindFriends by FriendManager; not stored in Firestore
        public bool IsOnline  { get; set; }
        public bool IsInRoom  { get; set; }
    }

    /// <summary>
    /// Firestore paths: friendRequests/{uid}/incoming/{fromUid} and friendRequests/{uid}/outgoing/{toUid}
    /// </summary>
    [Serializable]
    public class FriendRequestEntry
    {
        public string   Uid          { get; set; }
        public string   Username     { get; set; }
        public string   PhotonUserId { get; set; }
        public DateTime SentAt       { get; set; }
    }

    /// <summary>Result codes returned by FriendRequestManager.SendFriendRequestAsync.</summary>
    public enum FriendRequestResult
    {
        Success,
        UserNotFound,
        AlreadyFriends,
        RequestAlreadySent,
        IncomingRequestExists,  // the target already sent us a request, so accept it instead
        CannotAddSelf,
        FirestoreError
    }

    /// <summary>Firestore path: gameInvites/{targetUsername}/pending/{inviterUsername}</summary>
    [Serializable]
    public class GameInviteData
    {
        public string InviterUid      { get; set; }
        public string InviterUsername { get; set; }
        public string RoomName        { get; set; }
    }
}
