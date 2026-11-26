using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// One cube on the screen. It only knows how to look and how to animate (move, pop, punch, shake).
/// It knows nothing about the rules. BlastBoardView creates, positions and recycles these.
/// </summary>
public class BlastTile : MonoBehaviour
{
    private SpriteRenderer _renderer;
    private float _baseScale = 1f;
    private Coroutine _moveRoutine;
    private Coroutine _scaleRoutine;

    /// <summary>The board value this cube shows (a color index or BlastBoard.Bomb).</summary>
    public int Value { get; private set; }

    /// <summary>True while a move, pop, punch or shake is still running.</summary>
    public bool IsAnimating => _moveRoutine != null || _scaleRoutine != null;

    private void Awake()
    {
        _renderer = gameObject.AddComponent<SpriteRenderer>();
    }

    /// <summary>Prepares the cube for use. Also resets anything left over from its previous life in the pool.</summary>
    public void Setup(int value, Sprite sprite, Color color, float scale)
    {
        StopAllCoroutines();
        _moveRoutine = null;
        _scaleRoutine = null;

        gameObject.SetActive(true);
        _baseScale = scale;
        transform.localScale = Vector3.one * scale;

        SetLook(value, sprite, color);
    }

    public void SetLook(int value, Sprite sprite, Color color)
    {
        Value = value;
        _renderer.sprite = sprite;
        _renderer.color = color;
    }

    // A routine that ends before StartCoroutine returns would leave IsAnimating stuck on true
    // (the handle is assigned after the routine cleared it). A tiny minimum duration prevents that,
    // even if someone sets an animation time to 0 in the Inspector.
    private const float MinDuration = 0.01f;

    public void MoveTo(Vector3 target, float duration)
    {
        if (_moveRoutine != null)
            StopCoroutine(_moveRoutine);

        _moveRoutine = StartCoroutine(MoveRoutine(target, Mathf.Max(duration, MinDuration)));
    }

    /// <summary>Shrinks to nothing, then calls onDone (the view puts the cube back into the pool).</summary>
    public void Pop(float duration, Action<BlastTile> onDone)
    {
        StartScale(PopRoutine(Mathf.Max(duration, MinDuration), onDone));
    }

    /// <summary>A short "boing". Used when a cube changes into a bomb or is reshuffled.</summary>
    public void Punch(float duration)
    {
        StartScale(PunchRoutine(Mathf.Max(duration, MinDuration)));
    }

    /// <summary>A small side-to-side wiggle. Used for a tap that does nothing.</summary>
    public void Shake(float duration)
    {
        if (_moveRoutine != null)
            StopCoroutine(_moveRoutine);

        _moveRoutine = StartCoroutine(ShakeRoutine(Mathf.Max(duration, MinDuration)));
    }

    private void StartScale(IEnumerator routine)
    {
        if (_scaleRoutine != null)
            StopCoroutine(_scaleRoutine);

        _scaleRoutine = StartCoroutine(routine);
    }

    private IEnumerator MoveRoutine(Vector3 target, float duration)
    {
        Vector3 start = transform.position;

        for (float time = 0f; time < duration; time += Time.deltaTime)
        {
            // Squared easing starts slow and ends fast, like something falling.
            float t = time / duration;
            transform.position = Vector3.LerpUnclamped(start, target, t * t);
            yield return null;
        }

        transform.position = target;
        _moveRoutine = null;
    }

    private IEnumerator PopRoutine(float duration, Action<BlastTile> onDone)
    {
        for (float time = 0f; time < duration; time += Time.deltaTime)
        {
            transform.localScale = Vector3.one * (_baseScale * (1f - time / duration));
            yield return null;
        }

        transform.localScale = Vector3.zero;
        _scaleRoutine = null;
        onDone?.Invoke(this);
    }

    private IEnumerator PunchRoutine(float duration)
    {
        for (float time = 0f; time < duration; time += Time.deltaTime)
        {
            float bump = Mathf.Sin(time / duration * Mathf.PI) * 0.25f;
            transform.localScale = Vector3.one * (_baseScale * (1f + bump));
            yield return null;
        }

        transform.localScale = Vector3.one * _baseScale;
        _scaleRoutine = null;
    }

    private IEnumerator ShakeRoutine(float duration)
    {
        Vector3 origin = transform.position;

        for (float time = 0f; time < duration; time += Time.deltaTime)
        {
            float fade = 1f - time / duration;
            float offset = Mathf.Sin(time * 60f) * 0.08f * fade;
            transform.position = origin + new Vector3(offset, 0f, 0f);
            yield return null;
        }

        transform.position = origin;
        _moveRoutine = null;
    }
}
