// Standalone: no other scripts needed.
// Fits a UI panel into Screen.safeArea, so buttons are not hidden by notches, camera cutouts or rounded corners.
//
// Setup: create a full-screen child panel under your Canvas, put all your UI inside it
// and add this script to it. Keep the background image on a separate full-screen object.
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class ScreenSafeArea : MonoBehaviour
{
    [Header("Apply safe area to")]
    public bool left = true;
    public bool right = true;
    public bool top = true;
    public bool bottom = true;

    RectTransform rectTransform;
    Rect lastSafeArea;
    Vector2Int lastScreenSize;

    void OnEnable()
    {
        Apply();
    }

    // Screen size and safe area can change at runtime (rotation, foldable phones, the Editor Game view).
    void Update()
    {
        if (lastSafeArea != Screen.safeArea || lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height)
            Apply();
    }

    void Apply()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (Screen.width <= 0 || Screen.height <= 0)
            return;

        Rect safeArea = Screen.safeArea;
        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);

        Vector2 min = safeArea.position;
        Vector2 max = safeArea.position + safeArea.size;

        min.x = left ? min.x / Screen.width : 0f;
        min.y = bottom ? min.y / Screen.height : 0f;
        max.x = right ? max.x / Screen.width : 1f;
        max.y = top ? max.y / Screen.height : 1f;

        rectTransform.anchorMin = min;
        rectTransform.anchorMax = max;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
