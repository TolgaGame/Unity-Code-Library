// ─────────────────────────────────────────────────────────────────────────────
// AddFriendView.cs
// Attach to the AddFriendPopup GameObject.
// Handles the username input and send button logic.
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WolfTeen.Friends;

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

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        inputUsername.text = string.Empty;
        txtResult.text     = string.Empty;
        inputUsername.Select();
        inputUsername.ActivateInputField();
    }

    // ── Button handler — bağla: btnSend.onClick ────────────────────────────

    public async void OnPressedSend()
    {
        string username = inputUsername.text.Trim();

        if (string.IsNullOrEmpty(username))
        {
            ShowResult("Kullanıcı adı boş olamaz.", colorError);
            return;
        }

        btnSend.interactable    = false;
        inputUsername.interactable = false;
        txtResult.text          = "Aranıyor...";
        txtResult.color         = Color.white;

        FriendRequestResult result = await FriendRequestManager.Instance
            .SendFriendRequestAsync(username);

        btnSend.interactable       = true;
        inputUsername.interactable = true;

        switch (result)
        {
            case FriendRequestResult.Success:
                ShowResult($"'{username}' adlı oyuncuya istek gönderildi!", colorSuccess);
                inputUsername.text = string.Empty;
                break;
            case FriendRequestResult.UserNotFound:
                ShowResult("Kullanıcı bulunamadı.", colorError);
                break;
            case FriendRequestResult.AlreadyFriends:
                ShowResult("Bu kişi zaten arkadaşınız.", colorError);
                break;
            case FriendRequestResult.RequestAlreadySent:
                ShowResult("Bu kişiye zaten istek gönderildi.", colorError);
                break;
            case FriendRequestResult.IncomingRequestExists:
                ShowResult("Bu kişiden gelen bir isteğin var. Arkadaş isteklerini kontrol et.", colorError);
                break;
            case FriendRequestResult.CannotAddSelf:
                ShowResult("Kendinizi ekleyemezsiniz.", colorError);
                break;
            default:
                ShowResult("Bir hata oluştu, tekrar dene.", colorError);
                break;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

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
