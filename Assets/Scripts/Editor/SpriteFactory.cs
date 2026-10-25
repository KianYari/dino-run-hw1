using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class GeneratedArt
{
    public Sprite[] dinoRun = new Sprite[2];
    public Sprite dinoJump, dinoHit, dinoDead;
    public Sprite[] cacti = new Sprite[2];
    public Sprite[] bird = new Sprite[2];
    public Sprite star;
    public Sprite sky, clouds, mountains, hills, ground, white;
}

public static class SpriteFactory
{
    public const string ArtDir = "Assets/Art";
    const int PixelsPerUnit = 16;
    const int TileW = 320;
    const int ScreenH = 144;

    static Color32 C(int r, int g, int b, int a = 255) { return new Color32((byte)r, (byte)g, (byte)b, (byte)a); }

    static readonly Color32 Clear = C(0, 0, 0, 0);
    static readonly Color32 Ink = C(32, 30, 44);

    public static GeneratedArt Generate()
    {
        Directory.CreateDirectory(ArtDir);
        GeneratedArt a = new GeneratedArt();

        for (int pose = 0; pose < 5; pose++)
        {
            Sprite s = Save(Dino(pose), "dino_" + pose, SpriteAlignment.BottomCenter);
            switch (pose)
            {
                case 0: a.dinoRun[0] = s; break;
                case 1: a.dinoRun[1] = s; break;
                case 2: a.dinoJump = s; break;
                case 3: a.dinoHit = s; break;
                default: a.dinoDead = s; break;
            }
        }

        a.cacti[0] = Save(Cactus(14, new[] { 5 }, new[] { 16 }), "cactus_small", SpriteAlignment.BottomCenter);
        a.cacti[1] = Save(Cactus(26, new[] { 5, 18 }, new[] { 24, 18 }), "cactus_large", SpriteAlignment.BottomCenter);
        a.bird[0] = Save(Bird(true), "bird_0", SpriteAlignment.BottomCenter);
        a.bird[1] = Save(Bird(false), "bird_1", SpriteAlignment.BottomCenter);
        a.star = Save(Star(), "star", SpriteAlignment.BottomCenter);

        a.sky = Save(Sky(), "bg_sky", SpriteAlignment.Center);
        a.clouds = Save(Clouds(), "bg_clouds", SpriteAlignment.Center);
        a.mountains = Save(Mountains(), "bg_mountains", SpriteAlignment.Center);
        a.hills = Save(Hills(), "bg_hills", SpriteAlignment.Center);
        a.ground = Save(Ground(), "bg_ground", SpriteAlignment.Center);

        PixelCanvas w = new PixelCanvas(4, 4);
        w.Fill(C(255, 255, 255));
        a.white = Save(w, "white", SpriteAlignment.Center);

        return a;
    }

    static Sprite Save(PixelCanvas canvas, string name, SpriteAlignment align)
    {
        string path = ArtDir + "/" + name + ".png";
        Texture2D tex = canvas.ToTexture();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = PixelsPerUnit;
        imp.filterMode = FilterMode.Point;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.textureCompression = TextureImporterCompression.Uncompressed;

        TextureImporterSettings settings = new TextureImporterSettings();
        imp.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)align;
        imp.SetTextureSettings(settings);
        imp.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static PixelCanvas Dino(int pose)
    {
        // 0/1 run, 2 jump, 3 hit, 4 dead
        PixelCanvas p = new PixelCanvas(24, 22, 1, false);
        Color32 g = C(88, 179, 104), d = C(52, 128, 76), belly = C(176, 228, 150);
        Color32 white = C(255, 255, 255), black = Ink, red = C(220, 70, 70);

        p.Rect(2, 10, 4, 3, g); p.Rect(0, 9, 3, 2, g);
        p.Rect(4, 6, 12, 10, g);
        p.Rect(6, 6, 7, 6, belly);
        p.Rect(5, 16, 2, 1, d); p.Rect(8, 16, 2, 1, d);
        p.Rect(11, 16, 2, 1, d);
        p.Rect(12, 13, 5, 4, g);
        p.Rect(14, 15, 9, 7, g);
        p.Rect(17, 15, 6, 1, d);
        p.Rect(16, 10, 3, 2, d);

        p.Rect(18, 18, 3, 3, white);
        if (pose >= 3)
        {
            p.Set(18, 18, black); p.Set(20, 18, black); p.Set(19, 19, black);
            p.Set(18, 20, black); p.Set(20, 20, black);
            if (pose == 3) p.Rect(18, 15, 5, 1, red);
        }
        else
        {
            p.Set(20, 19, black); p.Set(20, 20, black);
        }

        switch (pose)
        {
            case 0: Leg(p, 6, 0, 6, g); Leg(p, 11, 3, 3, g); break;
            case 1: Leg(p, 6, 3, 3, g); Leg(p, 11, 0, 6, g); break;
            case 2: Leg(p, 6, 2, 4, g); Leg(p, 11, 2, 4, g); break;
            default: Leg(p, 6, 0, 6, g); Leg(p, 11, 0, 6, g); break;
        }

        p.Outline(Ink);
        return p;
    }

    static void Leg(PixelCanvas p, int x, int y, int h, Color32 c)
    {
        p.Rect(x, y, 3, h, c);
        p.Rect(x + 2, y, 2, 1, c);
    }

    static PixelCanvas Cactus(int width, int[] trunkX, int[] trunkH)
    {
        int maxH = 0;
        foreach (int h in trunkH) maxH = Mathf.Max(maxH, h);

        PixelCanvas p = new PixelCanvas(width, maxH, 1, false);
        Color32 green = C(64, 150, 78), light = C(110, 196, 112), dark = C(40, 104, 58);

        for (int i = 0; i < trunkX.Length; i++)
        {
            int x = trunkX[i], h = trunkH[i];
            p.Rect(x, 0, 4, h - 1, green);
            p.Rect(x + 1, h - 1, 2, 1, green);
            p.Rect(x + 1, 0, 1, h - 1, light);
            p.Rect(x + 3, 0, 1, h - 1, dark);

            int leftY = Mathf.RoundToInt(h * 0.35f), rightY = Mathf.RoundToInt(h * 0.5f);
            p.Rect(x - 3, leftY, 3, 2, green);
            p.Rect(x - 3, leftY, 2, 5, green);
            p.Rect(x + 4, rightY, 3, 2, green);
            p.Rect(x + 5, rightY, 2, 4, green);
        }

        p.Outline(Ink);
        return p;
    }

    static PixelCanvas Bird(bool wingUp)
    {
        PixelCanvas p = new PixelCanvas(26, 18, 1, false);
        Color32 body = C(92, 98, 140), wing = C(60, 66, 108), belly = C(176, 182, 214);
        Color32 beak = C(245, 170, 60), white = C(255, 255, 255);

        p.Rect(6, 6, 14, 6, body);
        p.Rect(8, 5, 10, 1, body);
        p.Rect(8, 6, 10, 2, belly);
        p.Rect(2, 8, 6, 5, body);
        p.Rect(0, 9, 2, 2, beak);
        p.Set(3, 11, white); p.Set(3, 10, Ink);
        p.Rect(20, 7, 5, 3, body);
        p.Rect(23, 6, 3, 5, wing);

        if (wingUp) { p.Rect(9, 12, 8, 5, wing); p.Rect(11, 17, 5, 1, wing); }
        else { p.Rect(9, 1, 8, 5, wing); p.Rect(11, 0, 5, 1, wing); }

        p.Outline(Ink);
        return p;
    }

    static PixelCanvas Star()
    {
        PixelCanvas p = new PixelCanvas(11, 11, 1, false);
        string[] rows =
        {
            ".....#.....",
            ".....#.....",
            "....#o#....",
            "....#o#....",
            "###########",
            ".####o####.",
            "..#######..",
            "...#####...",
            "..###.###..",
            "..##...##..",
            ".#.......#.",
        };
        p.Ascii(rows, new Dictionary<char, Color32> { { '#', C(255, 206, 52) }, { 'o', C(255, 244, 170) } });
        p.Outline(C(150, 90, 20));
        return p;
    }

    static PixelCanvas Sky()
    {
        PixelCanvas p = new PixelCanvas(256, ScreenH);
        Color32 top = C(88, 164, 232), bottom = C(214, 238, 246);
        int bandH = 12;
        for (int y = 0; y < ScreenH; y += bandH)
        {
            float t = (float)y / (ScreenH - bandH);
            Color c = Color.Lerp(top, bottom, t);
            p.Rect(0, y, 256, bandH, (Color32)c);
        }
        p.Ellipse(196, 104, 14, 14, C(255, 240, 190));
        p.Ellipse(196, 104, 10, 10, C(255, 226, 120));
        return p;
    }

    static PixelCanvas Clouds()
    {
        PixelCanvas p = new PixelCanvas(TileW, ScreenH);
        float[][] clouds =
        {
            new[] { 40f, 118f, 1.0f }, new[] { 120f, 100f, 0.7f }, new[] { 190f, 124f, 1.2f },
            new[] { 262f, 108f, 0.8f }, new[] { 300f, 126f, 0.6f },
        };
        foreach (float[] c in clouds)
        {
            float x = c[0], y = c[1], s = c[2];
            p.Ellipse(x, y - 1.5f * s, 15 * s, 5 * s, C(222, 236, 250), true);
            p.Ellipse(x, y, 14 * s, 5 * s, C(255, 255, 255), true);
            p.Ellipse(x - 7 * s, y + 3 * s, 7 * s, 5 * s, C(255, 255, 255), true);
            p.Ellipse(x + 3 * s, y + 5 * s, 8 * s, 6 * s, C(255, 255, 255), true);
        }
        return p;
    }

    static float Wave(int x, int cycles, float phase)
    {
        return Mathf.Sin(2f * Mathf.PI * cycles * x / TileW + phase);
    }

    static PixelCanvas Mountains()
    {
        PixelCanvas p = new PixelCanvas(TileW, ScreenH);
        Color32 rock = C(128, 150, 190), shade = C(108, 130, 172), snow = C(240, 246, 255);
        for (int x = 0; x < TileW; x++)
        {
            int h = Mathf.RoundToInt(62 + 16 * Wave(x, 2, 0.5f) + 9 * Wave(x, 5, 1.7f));
            p.Rect(x, 0, 1, h, rock);
            if (x % 2 == 0) p.Rect(x, 0, 1, h - 6, shade);
            if (h > 74) p.Rect(x, h - 4, 1, 4, snow);
        }
        return p;
    }

    static PixelCanvas Hills()
    {
        PixelCanvas p = new PixelCanvas(TileW, ScreenH);
        Color32 grass = C(110, 176, 104), deep = C(86, 148, 90);
        int[] treeX = { 50, 160, 270 };
        int[] heights = new int[TileW];
        for (int x = 0; x < TileW; x++)
        {
            heights[x] = Mathf.RoundToInt(46 + 9 * Wave(x, 3, 2f) + 5 * Wave(x, 8, 0f));
            p.Rect(x, 0, 1, heights[x], grass);
            p.Rect(x, 0, 1, heights[x] - 8, deep);
        }
        foreach (int tx in treeX)
        {
            int baseY = heights[tx];
            p.Rect(tx, baseY - 4, 2, 10, C(112, 78, 52));
            p.Ellipse(tx + 1, baseY + 11, 6, 7, C(62, 128, 76));
            p.Ellipse(tx + 3, baseY + 9, 3, 4, C(84, 150, 92));
        }
        return p;
    }

    static PixelCanvas Ground()
    {
        PixelCanvas p = new PixelCanvas(TileW, 24);
        System.Random rng = new System.Random(7);
        Color32 dirt = C(150, 112, 76), seam = C(112, 82, 58), sand = C(226, 200, 146), edge = C(242, 222, 172);

        p.Fill(dirt);
        p.Rect(0, 19, TileW, 1, seam);
        p.Rect(0, 20, TileW, 4, sand);
        p.Rect(0, 23, TileW, 1, edge);

        for (int i = 0; i < 70; i++)
        {
            int x = rng.Next(TileW), y = rng.Next(2, 18);
            Color32 c = rng.Next(2) == 0 ? C(120, 88, 60) : C(176, 138, 96);
            p.Rect(x, y, rng.Next(1, 3), 1, c);
        }
        for (int i = 0; i < 40; i++)
            p.Set(rng.Next(TileW), rng.Next(20, 23), C(206, 178, 124));

        return p;
    }
}
