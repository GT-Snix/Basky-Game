using UnityEngine;

// A line of text drawn with the pixel font. Rebuilds its sprite only when the text changes.
[RequireComponent(typeof(SpriteRenderer))]
public class PixelText : MonoBehaviour
{
    SpriteRenderer sr;
    string current;
    Color32 color;
    Vector2 anchor;

    public SpriteRenderer Renderer => sr;

    public static PixelText Create(string name, Transform parent, Vector2 pos, int scale, Color32 color,
        Vector2 anchor, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Snap(pos);
        go.transform.localScale = new Vector3(scale, scale, 1);
        var text = go.AddComponent<PixelText>();
        text.sr = go.GetComponent<SpriteRenderer>();
        text.sr.sortingOrder = order;
        text.color = color;
        text.anchor = anchor;
        return text;
    }

    public static Vector3 Snap(Vector2 p) =>
        new Vector3(Mathf.Round(p.x * PixelArt.PPU) / PixelArt.PPU, Mathf.Round(p.y * PixelArt.PPU) / PixelArt.PPU, 0);

    public void Set(string text)
    {
        if (text == current) return;
        current = text;
        Rebuild();
    }

    public void Set(string text, Color32 newColor)
    {
        color = newColor;
        current = null;
        Set(text);
    }

    void Rebuild()
    {
        Release();
        sr.sprite = PixelArt.Text(current, color, anchor);
    }

    void Release()
    {
        if (sr.sprite == null) return;
        Destroy(sr.sprite.texture);
        Destroy(sr.sprite);
        sr.sprite = null;
    }

    void OnDestroy() => Release();
}
