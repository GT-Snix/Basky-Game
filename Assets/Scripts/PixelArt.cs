using System.Collections.Generic;
using UnityEngine;

// Builds every sprite in the game procedurally, so the project needs no imported art.
public static class PixelArt
{
    public const int PPU = 16;

    // Sweetie-16 inspired palette.
    static readonly Dictionary<char, Color32> Palette = new Dictionary<char, Color32>
    {
        { 'k', new Color32(0x1a, 0x1c, 0x2c, 0xff) }, // outline
        { 'p', new Color32(0x5d, 0x27, 0x5d, 0xff) },
        { 'r', new Color32(0xb1, 0x3e, 0x53, 0xff) },
        { 'O', new Color32(0xef, 0x7d, 0x57, 0xff) },
        { 'y', new Color32(0xff, 0xcd, 0x75, 0xff) },
        { 'g', new Color32(0xa7, 0xf0, 0x70, 0xff) },
        { 'G', new Color32(0x38, 0xb7, 0x64, 0xff) },
        { 't', new Color32(0x25, 0x71, 0x79, 0xff) },
        { 'n', new Color32(0x29, 0x36, 0x6f, 0xff) },
        { 'b', new Color32(0x3b, 0x5d, 0xc9, 0xff) },
        { 'c', new Color32(0x41, 0xa6, 0xf6, 0xff) },
        { 'C', new Color32(0x73, 0xef, 0xf7, 0xff) },
        { 'w', new Color32(0xf4, 0xf4, 0xf4, 0xff) },
        { 'l', new Color32(0x94, 0xb0, 0xc2, 0xff) },
        { 's', new Color32(0x56, 0x6c, 0x86, 0xff) },
        { 'd', new Color32(0x33, 0x3c, 0x57, 0xff) },
        { 'h', new Color32(0xc2, 0x8a, 0x4a, 0xff) }, // light wood
        { 'B', new Color32(0x8b, 0x5a, 0x2b, 0xff) }, // wood
        { 'H', new Color32(0x5a, 0x3a, 0x1e, 0xff) }, // dark wood
    };

    public static Color32 C(char key) => Palette[key];

    static Texture2D NewTexture(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        return tex;
    }

    static Sprite ToSprite(Texture2D tex, Color32[] px, Vector2 pivot)
    {
        tex.SetPixels32(px);
        tex.Apply(false, false);
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, PPU, 0, SpriteMeshType.FullRect);
    }

    // Rows are written top-to-bottom; '.' is transparent.
    public static Sprite FromRows(string[] rows, Vector2? pivot = null)
    {
        int h = rows.Length, w = rows[0].Length;
        var tex = NewTexture(w, h);
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            string row = rows[h - 1 - y];
            for (int x = 0; x < w; x++)
            {
                char c = x < row.Length ? row[x] : '.';
                px[y * w + x] = c == '.' ? new Color32(0, 0, 0, 0) : Palette[c];
            }
        }
        return ToSprite(tex, px, pivot ?? new Vector2(0.5f, 0.5f));
    }

    public static Sprite FromFunc(int w, int h, System.Func<int, int, Color32> f, Vector2? pivot = null)
    {
        var tex = NewTexture(w, h);
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = f(x, y);
        return ToSprite(tex, px, pivot ?? new Vector2(0.5f, 0.5f));
    }

    static uint Hash(int x, int y)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263);
            h = (h ^ (h >> 13)) * 1274126177;
            return h ^ (h >> 16);
        }
    }

    // ---------- Coin ----------

    static readonly string[] CoinRows =
    {
        "....kkkk....",
        "..kkyyyykk..",
        ".kyyyyyyyyk.",
        ".kywyOOyyOk.",
        "kywyOyyOyyOk",
        "kyyyOyyOyyOk",
        "kyyyOyyOyyOk",
        "kyyyOyyOyyOk",
        ".kyyyOOyyOk.",
        ".kOyyyyyyOk.",
        "..kkOOOOkk..",
        "....kkkk....",
    };

    // Squashes the coin horizontally in pixel space to fake a spin, then re-outlines the edges.
    static string[] SquashX(string[] rows, int newWidth)
    {
        int w = rows[0].Length;
        int pad = (w - newWidth) / 2;
        var result = new string[rows.Length];
        for (int r = 0; r < rows.Length; r++)
        {
            var chars = new char[w];
            for (int x = 0; x < w; x++) chars[x] = '.';
            for (int x = 0; x < newWidth; x++)
            {
                int src = Mathf.FloorToInt((x + 0.5f) * w / newWidth);
                chars[pad + x] = rows[r][src];
            }
            int first = System.Array.FindIndex(chars, c => c != '.');
            int last = System.Array.FindLastIndex(chars, c => c != '.');
            if (first >= 0) { chars[first] = 'k'; chars[last] = 'k'; }
            result[r] = new string(chars);
        }
        return result;
    }

    public static Sprite[] CoinFrames()
    {
        return new[]
        {
            FromRows(CoinRows),
            FromRows(SquashX(CoinRows, 8)),
            FromRows(SquashX(CoinRows, 4)),
            FromRows(SquashX(CoinRows, 8)),
        };
    }

    // ---------- Bomb ----------

    public static Sprite[] BombFrames()
    {
        string[] body =
        {
            "...kssk.....",
            "...kkkk.....",
            ".kkddddkk...",
            ".kdlddddk...",
            "kdlwdddddk..",
            "kdlddddddk..",
            "kddddddddk..",
            "kddddddsdk..",
            ".kddddsdk...",
            ".kkddddkk...",
            "...kkkk.....",
        };
        string[] sparkA = { ".........y..", "........yOy.", ".......k.y..", "......k....." };
        string[] sparkB = { "........w.y.", ".........O..", ".......kw.w.", "......k....." };
        Sprite Build(string[] spark)
        {
            var rows = new List<string>(spark);
            rows.AddRange(body);
            return FromRows(rows.ToArray());
        }
        return new[] { Build(sparkA), Build(sparkB) };
    }

    // ---------- Basket ----------

    public const int BasketW = 32, BasketH = 14;

    public static Sprite Basket()
    {
        const int rimBottom = 10;
        return FromFunc(BasketW, BasketH, (x, y) =>
        {
            int inset = y >= rimBottom ? 0 : (rimBottom - y) / 3 + 1;
            int left = inset, right = BasketW - 1 - inset;
            if (x < left || x > right) return new Color32(0, 0, 0, 0);
            bool edge = x == left || x == right || y == 0 || y == BasketH - 1;
            if (edge) return C('k');
            if (y >= rimBottom)
            {
                if (y == rimBottom) return C('H');
                return y == BasketH - 2 ? C('y') : C('h');
            }
            if (y % 4 == 1) return C('H');
            return ((x / 2 + y / 4) % 2 == 0) ? C('h') : C('B');
        }, new Vector2(0.5f, 0f));
    }

    // ---------- Hearts ----------

    public static Sprite Heart(bool full)
    {
        string[] rows =
        {
            ".kk.kk.",
            "krwkrrk",
            "krrrrrk",
            ".krrrk.",
            "..krk..",
            "...k...",
        };
        if (!full)
            for (int i = 0; i < rows.Length; i++)
                rows[i] = rows[i].Replace('r', 'd').Replace('w', 's');
        return FromRows(rows);
    }

    // ---------- World ----------

    public static Sprite GroundTile(bool top)
    {
        return FromFunc(16, 16, (x, y) =>
        {
            uint n = Hash(x, y);
            if (top)
            {
                int grassDepth = 3 + (int)(Hash(x, 99) % 2);
                if (y == 15) return C('g');
                if (y >= 16 - grassDepth) return (n % 5 == 0) ? C('g') : C('G');
                if (y == 15 - grassDepth) return C('t');
            }
            if (n % 11 == 0) return C('h');
            if (n % 7 == 0) return C('H');
            return C('B');
        }, new Vector2(0.5f, 1f));
    }

    // A tall sky with dithered colour bands, pixel clouds and rolling hills.
    public static Sprite Background(int w, int h, int horizon)
    {
        Color32[] bands = { C('C'), C('c'), C('b'), C('n') };
        // Bayer 4x4 for ordered dithering between sky bands.
        int[] bayer = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
        return FromFunc(w, h, (x, y) =>
        {
            // Hills.
            float hillFar = horizon + 26 + 10 * Mathf.Sin(x * 0.035f) + 5 * Mathf.Sin(x * 0.11f + 1.3f);
            float hillNear = horizon + 12 + 8 * Mathf.Sin(x * 0.05f + 2f) + 3 * Mathf.Sin(x * 0.17f);
            if (y < hillNear) return C('G');
            if (y < hillNear + 1) return C('g');
            if (y < hillFar) return C('t');

            // Clouds: a few soft blobs.
            for (int i = 0; i < 6; i++)
            {
                float cx = (i * 97 + 30) % w;
                float cy = horizon + 60 + (i * 23) % 90;
                float dx = (x - cx) / 18f, dy = (y - cy) / 6f;
                float d = dx * dx + dy * dy;
                float bump = Mathf.Sin((x + i * 13) * 0.4f) * 0.15f;
                if (d < 1f + bump) return y < cy - 2 ? C('l') : C('w');
            }

            // Sky bands, darker toward the top.
            float t = Mathf.InverseLerp(hillFar, h, y) * (bands.Length - 1);
            int band = Mathf.Clamp((int)t, 0, bands.Length - 2);
            float frac = t - band;
            float threshold = bayer[(y % 4) * 4 + (x % 4)] / 16f;
            return frac > threshold ? bands[band + 1] : bands[band];
        }, new Vector2(0.5f, 0f));
    }

    public static Sprite[] ExplosionFrames()
    {
        const int size = 32;
        var frames = new Sprite[6];
        for (int f = 0; f < frames.Length; f++)
        {
            float outer = 4 + f * 2.6f;
            float inner = f < 2 ? 0 : (f - 1) * 2.8f;
            int frame = f;
            frames[f] = FromFunc(size, size, (x, y) =>
            {
                float dx = x - size / 2 + 0.5f, dy = y - size / 2 + 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) + (Hash(x, y + frame * 50) % 3) * 0.6f;
                if (d > outer || d < inner) return new Color32(0, 0, 0, 0);
                float t = (d - inner) / Mathf.Max(0.01f, outer - inner);
                if (frame >= 4) return t > 0.5f ? C('s') : C('d');
                if (t < 0.35f) return C('w');
                if (t < 0.6f) return C('y');
                if (t < 0.85f) return C('O');
                return C('r');
            });
        }
        return frames;
    }

    public static Sprite Solid(Color32 c) => FromFunc(4, 4, (x, y) => c);

    // ---------- Pixel font (3x5) ----------

    // Each glyph is five rows of three pixels, top to bottom.
    static readonly Dictionary<char, string> Glyphs = new Dictionary<char, string>
    {
        { '0', "###/#.#/#.#/#.#/###" }, { '1', ".#./##./.#./.#./###" }, { '2', "###/..#/###/#../###" }, { '3', "###/..#/.##/..#/###" },
        { '4', "#.#/#.#/###/..#/..#" }, { '5', "###/#../###/..#/###" }, { '6', "###/#../###/#.#/###" }, { '7', "###/..#/..#/.#./.#." },
        { '8', "###/#.#/###/#.#/###" }, { '9', "###/#.#/###/..#/###" }, { 'A', ".#./#.#/###/#.#/#.#" }, { 'B', "##./#.#/##./#.#/##." },
        { 'C', ".##/#../#../#../.##" }, { 'D', "##./#.#/#.#/#.#/##." }, { 'E', "###/#../##./#../###" }, { 'F', "###/#../##./#../#.." },
        { 'G', ".##/#../#.#/#.#/.##" }, { 'H', "#.#/#.#/###/#.#/#.#" }, { 'I', "###/.#./.#./.#./###" }, { 'J', "..#/..#/..#/#.#/.#." },
        { 'K', "#.#/#.#/##./#.#/#.#" }, { 'L', "#../#../#../#../###" }, { 'M', "#.#/###/###/#.#/#.#" }, { 'N', "##./#.#/#.#/#.#/#.#" },
        { 'O', ".#./#.#/#.#/#.#/.#." }, { 'P', "##./#.#/##./#../#.." }, { 'Q', ".#./#.#/#.#/##./.##" }, { 'R', "##./#.#/##./#.#/#.#" },
        { 'S', ".##/#../.#./..#/##." }, { 'T', "###/.#./.#./.#./.#." }, { 'U', "#.#/#.#/#.#/#.#/###" }, { 'V', "#.#/#.#/#.#/#.#/.#." },
        { 'W', "#.#/#.#/###/###/#.#" }, { 'X', "#.#/#.#/.#./#.#/#.#" }, { 'Y', "#.#/#.#/.#./.#./.#." }, { 'Z', "###/..#/.#./#../###" },
        { '+', ".../.#./###/.#./..." }, { '-', ".../.../###/.../..." }, { ':', ".../.#./.../.#./..." }, { '!', ".#./.#./.#./.../.#." },
        { 'x', ".../#.#/.#./#.#/..." }, { '/', "..#/..#/.#./#../#.." }, { ' ', ".../.../.../.../..." },
    };

    // Renders text with a 1px dark outline. Returns a sprite one texel per font pixel.
    public static Sprite Text(string text, Color32 color, Vector2 pivot)
    {
        text = text ?? "";
        int w = Mathf.Max(1, text.Length * 4 - 1) + 2;
        int h = 7;
        var on = new bool[w, h];
        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (!Glyphs.TryGetValue(ch, out var g) && !Glyphs.TryGetValue(char.ToUpperInvariant(ch), out g)) continue;
            var rows = g.Split('/');
            for (int gy = 0; gy < 5; gy++)
                for (int gx = 0; gx < 3; gx++)
                    if (rows[gy][gx] == '#')
                        on[1 + i * 4 + gx, h - 2 - gy] = true;
        }
        Color32 outline = C('k');
        // Keep the pivot on a texel boundary so text stays crisp.
        var alignedPivot = new Vector2(Mathf.Floor(w * pivot.x) / w, Mathf.Floor(h * pivot.y) / h);
        return FromFunc(w, h, (x, y) =>
        {
            if (on[x, y]) return color;
            for (int oy = -1; oy <= 1; oy++)
                for (int ox = -1; ox <= 1; ox++)
                {
                    int nx = x + ox, ny = y + oy;
                    if (nx >= 0 && ny >= 0 && nx < w && ny < h && on[nx, ny]) return outline;
                }
            return new Color32(0, 0, 0, 0);
        }, alignedPivot);
    }
}
