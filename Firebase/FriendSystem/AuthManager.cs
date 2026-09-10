// ─────────────────────────────────────────────────────────────────────────────
// AuthManager.cs
// Responsibilities:
//   1. Listen to bl_LoginPro.OnLogin (MFPS login system).
//   2. On MFPS login: sign in to Firebase Anonymously to get a stable UID.
//   3. Write/load users/{uid} in Firestore using the MFPS NickName.
//   4. Set PhotonNetwork.NickName = NickName.
//   5. Expose events so FriendManager / FriendRequestManager can react.
//
// Why Anonymous Auth?
//   MFPS uses its own PHP/MySQL backend — not Firebase Auth.
//   Anonymous Auth gives us a stable Firebase UID per device install,
//   which satisfies Firestore security rules (request.auth != null).
//
// Attach to a persistent GameObject. FirebaseManager calls Initialize().
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using WolfTeen.Friends;
using UserProfile = WolfTeen.Friends.UserProfile;

public class AuthManager : PersistentSingleton<AuthManager>, IConnectionCallbacks
{
    // ── Public state ─────────────────────────────────────────────────────────

    /// <summary>Currently authenticated local user. Null until sign-in succeeds.</summary>
    public UserProfile CurrentUser { get; private set; }

    /// <summary>True once PhotonNetwork has connected to the Master Server.</summary>
    public bool IsPhotonConnected { get; private set; }

    public bool IsAuthenticated => CurrentUser != null;

    // ── Events ───────────────────────────────────────────────────────────────

    /// <summary>Fired on the main thread once profile is ready.</summary>
    public static event Action<UserProfile> OnSignInComplete;

    /// <summary>Fired when sign-in fails.</summary>
    public static event Action<string> OnSignInFailed;

    /// <summary>Fired when the user logs out.</summary>
    public static event Action OnSignedOut;

    /// <summary>Fired once Photon connects to the Master Server.</summary>
    public static event Action OnPhotonConnected;

    /// <summary>Fired when Photon disconnects unexpectedly.</summary>
    public static event Action<DisconnectCause> OnPhotonDisconnected;

    // ── Internals ────────────────────────────────────────────────────────────

    private bool _profileLoadInProgress;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDestroy()
    {
        UnsubscribeFromMFPS();
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    // ── Initialization ───────────────────────────────────────────────────────

    /// <summary>
    /// Called from FirebaseManager.InitializeFirebase() once Firebase is ready.
    /// </summary>
    public void Initialize()
    {
        SubscribeToMFPS();

        // Already logged in before Firebase finished initializing?
        if (bl_DataBase.Instance != null && bl_DataBase.Instance.isLogged)
            HandleMFPSLogin();
    }

    // ── MFPS login events ─────────────────────────────────────────────────────

    private void SubscribeToMFPS()
    {
        if (bl_LoginPro.Instance == null)
        {
            Debug.LogWarning("[AuthManager] bl_LoginPro.Instance not found. Will retry on next Initialize call.");
            return;
        }

        bl_LoginPro.Instance.OnLogin.AddListener(HandleMFPSLogin);
        bl_LoginPro.Instance.OnLogOut.AddListener(HandleMFPSLogout);
    }

    private void UnsubscribeFromMFPS()
    {
        if (bl_LoginPro.Instance == null) return;
        bl_LoginPro.Instance.OnLogin.RemoveListener(HandleMFPSLogin);
        bl_LoginPro.Instance.OnLogOut.RemoveListener(HandleMFPSLogout);
    }

    private void HandleMFPSLogin()
    {
        if (_profileLoadInProgress) return;

        if (bl_DataBase.Instance == null || bl_DataBase.Instance.LocalUser == null)
        {
            OnSignInFailed?.Invoke("bl_DataBase.LocalUser is null after login.");
            return;
        }

        string mfpsNickName = bl_DataBase.Instance.LocalUser.NickName;

        if (string.IsNullOrEmpty(mfpsNickName))
        {
            // Guest / nickname not yet set — fall back to LoginName
            mfpsNickName = bl_DataBase.Instance.LocalUser.LoginName;
        }

        string username = SanitizeUsername(mfpsNickName);

        _profileLoadInProgress = true;
        SignInAnonymouslyAndSaveProfileAsync(username).ContinueWithOnMainThread(task =>
        {
            _profileLoadInProgress = false;

            if (task.IsFaulted || task.IsCanceled)
            {
                string msg = task.Exception?.GetBaseException().Message ?? "Cancelled";
                Debug.LogError($"[AuthManager] Profile save failed: {msg}");
                OnSignInFailed?.Invoke(msg);
                return;
            }

            CurrentUser = task.Result;
            PhotonNetwork.NickName = CurrentUser.Username;
            Debug.Log($"[AuthManager] Signed in as '{CurrentUser.Username}' (uid={CurrentUser.Uid}).");
            OnSignInComplete?.Invoke(CurrentUser);
        });
    }

    private void HandleMFPSLogout()
    {
        CurrentUser = null;
        FirebaseAuth.DefaultInstance.SignOut();
        OnSignedOut?.Invoke();
        Debug.Log("[AuthManager] User signed out.");
    }

    // ── Firebase Anonymous Auth + Firestore profile ───────────────────────────

    private async Task<UserProfile> SignInAnonymouslyAndSaveProfileAsync(string username)
    {
        FirebaseAuth auth = FirebaseAuth.DefaultInstance;

        // Reuse existing anonymous session if available.
        if (auth.CurrentUser == null)
        {
            var authResult = await auth.SignInAnonymouslyAsync();
        }

        string uid = auth.CurrentUser.UserId;

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

        // Use the stable username as the document ID instead of the anonymous UID.
        // Anonymous Auth generates a new UID on every reinstall / cache clear, which
        // would create duplicate documents for the same player.
        DocumentReference  userRef = db.Collection("users").Document(username);
        DocumentSnapshot   snap    = await userRef.GetSnapshotAsync();

        if (snap.Exists)
        {
            // Profile exists — refresh the anonymous auth UID in case it rotated.
            await userRef.UpdateAsync(new Dictionary<string, object>
            {
                { "username",     username },
                { "photonUserId", username },
                { "firebaseUid",  uid      }
            });
        }
        else
        {
            // First time — create the profile.
            await userRef.SetAsync(new Dictionary<string, object>
            {
                { "username",     username },
                { "photonUserId", username },
                { "firebaseUid",  uid      }
            });
        }

        // Expose username as Uid so the rest of the system (FriendManager,
        // FriendRequestManager) uses a stable, per-player document key.
        return new UserProfile
        {
            Uid          = username,
            Username     = username,
            PhotonUserId = username
        };
    }

    // ── Username sanitizer ────────────────────────────────────────────────────

    /// <summary>
    /// Removes characters illegal in Firestore document IDs and Photon userIds.
    /// Keeps letters, digits, underscores, and hyphens.
    /// </summary>
    public static string SanitizeUsername(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Player";

        var sb = new StringBuilder(raw.Length);
        foreach (char c in raw)
        {
            if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
                sb.Append(c);
        }

        string result = sb.ToString().Trim('-', '_');
        return result.Length > 0 ? result : "Player";
    }

    // ── IConnectionCallbacks (Photon) ─────────────────────────────────────────

    public void OnConnected() { }

    public void OnConnectedToMaster()
    {
        IsPhotonConnected = true;
        Debug.Log("[AuthManager] Photon connected to Master Server.");
        OnPhotonConnected?.Invoke();
    }

    public void OnDisconnected(DisconnectCause cause)
    {
        IsPhotonConnected = false;

        // DisconnectByClientLogic is bl_Lobby's intentional reconnect — not an error.
        if (cause != DisconnectCause.DisconnectByClientLogic)
            Debug.LogWarning($"[AuthManager] Photon disconnected: {cause}");

        OnPhotonDisconnected?.Invoke(cause);
    }

    public void OnRegionListReceived(RegionHandler regionHandler)                   { }
    public void OnCustomAuthenticationResponse(Dictionary<string, object> data)     { }
    public void OnCustomAuthenticationFailed(string debugMessage)                   { }
}
