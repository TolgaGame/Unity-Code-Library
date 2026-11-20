using UnityEngine;

/// <summary>
/// Example that combines the input scripts: moves an object with the on-screen joystick,
/// and falls back to keyboard / gamepad (InputReader) when the joystick is not touched.
/// Also reacts to swipes (optional): a swipe dashes the object in that direction.
/// </summary>
public class InputDemoMover : MonoBehaviour
{
    [SerializeField] private float speed = 5f;

    [Header("Optional input sources")]
    [SerializeField] private VirtualJoystick joystick;
    [SerializeField] private SwipeDetector swipeDetector;
    [SerializeField] private float dashDistance = 2f;

    private void OnEnable()
    {
        if (swipeDetector != null)
        {
            swipeDetector.OnSwipe += OnSwipe;
        }
    }

    private void OnDisable()
    {
        if (swipeDetector != null)
        {
            swipeDetector.OnSwipe -= OnSwipe;
        }
    }

    private void Update()
    {
        Vector2 input = joystick != null && joystick.Direction != Vector2.zero
            ? joystick.Direction
            : InputReader.ReadMove();

        transform.Translate(new Vector3(input.x, input.y, 0f) * (speed * Time.deltaTime));
    }

    private void OnSwipe(SwipeDirection direction)
    {
        Vector3 offset;
        switch (direction)
        {
            case SwipeDirection.Up: offset = Vector3.up; break;
            case SwipeDirection.Down: offset = Vector3.down; break;
            case SwipeDirection.Left: offset = Vector3.left; break;
            default: offset = Vector3.right; break;
        }

        transform.position += offset * dashDistance;
    }
}
