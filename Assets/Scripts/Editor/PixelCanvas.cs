using UnityEngine;

public class PixelCanvas
{
    public readonly int Width;
    public readonly int Height;

    readonly Color32[] px;
    readonly int margin;
    readonly int marginBottom;

    public PixelCanvas(int width, int height, int margin = 0, bool bottomMargin = true)
    {
        this.margin = margin;
        marginBottom = bottomMargin ? margin : 0;
        Width = width + 2 * margin;
        Height = height + marginBottom + margin;
        px = new Color32[Width * Height];
    }

    public void Set(int x, int y, Color32 c)
    {
        x += margin;
        y += marginBottom;
        if (x < 0 || y < 0 || x >= Width || y >= Height) return;
        px[y * Width + x] = c;
    }

    public void Rect(int x, int y, int w, int h, Color32 c)
    {
        for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
                Set(x + i, y + j, c);
    }

    public void Fill(Color32 c)
    {
        for (int i = 0; i < px.Length; i++) px[i] = c;
    }

    public void Ellipse(float cx, float cy, float rx, float ry, Color32 c, bool wrap = false)
    {
        int[] shifts = wrap ? new[] { -Width, 0, Width } : new[] { 0 };
        foreach (int s in shifts)
        {
            float ccx = cx + s;
            for (int y = Mathf.FloorToInt(cy - ry); y <= Mathf.CeilToInt(cy + ry); y++)
                for (int x = Mathf.FloorToInt(ccx - rx); x <= Mathf.CeilToInt(ccx + rx); x++)
                {
                    float dx = (x - ccx) / rx;
                    float dy = (y - cy) / ry;
                    if (dx * dx + dy * dy <= 1f) Set(x, y, c);
                }
        }
    }

    public void Outline(Color32 c)
    {
        Color32[] src = (Color32[])px.Clone();
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                if (src[y * Width + x].a != 0) continue;
                bool near =
                    (x > 0 && src[y * Width + x - 1].a != 0) ||
                    (x < Width - 1 && src[y * Width + x + 1].a != 0) ||
                    (y > 0 && src[(y - 1) * Width + x].a != 0) ||
                    (y < Height - 1 && src[(y + 1) * Width + x].a != 0);
                if (near) px[y * Width + x] = c;
            }
    }

    public void Ascii(string[] rows, System.Collections.Generic.Dictionary<char, Color32> palette)
    {
        for (int r = 0; r < rows.Length; r++)
            for (int x = 0; x < rows[r].Length; x++)
                if (palette.TryGetValue(rows[r][x], out Color32 c))
                    Set(x, rows.Length - 1 - r, c);
    }

    public Texture2D ToTexture()
    {
        Texture2D tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }
}
