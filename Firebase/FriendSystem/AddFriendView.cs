using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WolfTeen.Friends;

/// <summary>Popup that takes a username and sends a friend request.</summary>
public class AddFriendView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_InputField inputUsername;
    [SerializeField] private Button         btnSend;
    [SerializeField] private TextMeshProUGUI txtResult;

    [Header("Result Colors")]
    [SerializeField] private Color colorSuccess = new Color(0.2f, 0.85f, 0.2f);
    [SerializeField] private Color colorError   = new Color(0.9f, 0.3f, 0.3f);

    private Coroutine _clearCoroutine;

    private void OnEnable()
    {
        inputUsername.text = string.Empty;
        txtResult.text     = string.Empty;
        inputUsername.Select();
        inputUsername.ActivateInputField();
    }

    /// <summary>Hook up to btnSend.onClick.</summary>
    public async void OnPressedSend()
    {
        string username = inputUsername.text.Trim();

        if (string.IsNullOrEmpty(username))
        {
            ShowResult("Username cannot be empty.", colorError);
            return;
        }

        btnSend.interactable    = false;
        inputUsername.interactable = false;
        txtResult.text          = "Searching...";
        txtResult.color         = Color.white;

        FriendRequestResult result = await FriendRequestManager.Instance
            .SendFriendRequestAsync(username);

        btnSend.interactable       = true;
        inputUsername.interactable = true;

        switch (result)
        {
            case FriendRequestResult.Success:
                ShowResult($"Friend request sent to '{username}'!", colorSuccess);
                inputUsername.text = string.Empty;
                break;
            case FriendRequestResult.UserNotFound:
                ShowResult("User not found.", colorError);
                break;
            case FriendRequestResult.AlreadyFriends:
                ShowResult("You are already friends with this player.", colorError);
                break;
            case FriendRequestResult.RequestAlreadySent:
                ShowResult("You already sent a request to this player.", colorError);
                break;
            case FriendRequestResult.IncomingRequestExists:
                ShowResult("This player already sent you a request. Check your friend requests.", colorError);
                break;
            case FriendRequestResult.CannotAddSelf:
                ShowResult("You cannot add yourself.", colorError);
                break;
            default:
                ShowResult("Something went wrong, please try again.", colorError);
                break;
        }
    }

    private void ShowResult(string message, Color color)
    {
        txtResult.text  = message;
        txtResult.color = color;

        if (_clearCoroutine != null) StopCoroutine(_clearCoroutine);
        _clearCoroutine = StartCoroutine(ClearResultAfterDelay(4f));
    }

    private IEnumerator ClearResultAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        txtResult.text = string.Empty;
    }
}
