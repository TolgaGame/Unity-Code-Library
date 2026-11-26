using UnityEngine;

/// <summary>
/// Makes the cube and bomb sprites with code, so the example needs no art files.
/// Replace these with your own sprites when you build a real game.
/// </summary>
public static class BlastSprites
{
    private const int Size = 64;

    private static Sprite _cube;
    private static Sprite _bomb;

    /// <summary>White rounded square. Tint it with SpriteRenderer.color.</summary>
    public static Sprite Cube
    {
        get
        {
            if (_cube == null)
                _cube = Create(CubePixel);
            return _cube;
        }
    }

    /// <summary>Dark circle with an orange core. Do not tint it.</summary>
    public static Sprite Bomb
    {
        get
        {
            if (_bomb == null)
                _bomb = Create(BombPixel);
            return _bomb;
        }
    }

    private delegate Color PixelFunc(float x, float y);

    // x and y go from -1 to 1 across the sprite. One sprite unit = one grid cell (pixelsPerUnit = Size).
    private static Sprite Create(PixelFunc pixel)
    {
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        var colors = new Color[Size * Size];
        for (int py = 0; py < Size; py++)
        {
            for (int px = 0; px < Size; px++)
            {
                float x = (px + 0.5f) / Size * 2f - 1f;
                float y = (py + 0.5f) / Size * 2f - 1f;
                colors[py * Size + px] = pixel(x, y);
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
    }

    private static Color CubePixel(float x, float y)
    {
        const float radius = 0.35f;

        // Distance to a rounded box: negative inside, positive outside.
        float qx = Mathf.Abs(x) - (1f - radius);
        float qy = Mathf.Abs(y) - (1f - radius);
        float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
        float distance = outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;

        // Soft edge of about one pixel, and a slightly darker bottom for a little depth.
        float alpha = Mathf.Clamp01(0.5f - distance * Size * 0.5f);
        float shade = Mathf.Lerp(0.8f, 1f, (y + 1f) * 0.5f);

        return new Color(shade, shade, shade, alpha);
    }

    private static Color BombPixel(float x, float y)
    {
        float distance = Mathf.Sqrt(x * x + y * y);
        float alpha = Mathf.Clamp01(0.5f - (distance - 1f) * Size * 0.5f);

        Color body = new Color(0.16f, 0.16f, 0.2f);
        Color core = new Color(1f, 0.55f, 0.1f);
        Color color = Color.Lerp(core, body, Mathf.Clamp01((distance - 0.38f) * 12f));
        color.a = alpha;

        return color;
    }
}
