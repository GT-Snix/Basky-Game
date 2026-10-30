using UnityEngine;

// Plays a sprite sequence once, then removes itself.
public class FrameEffect : MonoBehaviour
{
    Sprite[] frames;
    float frameTime, age;
    SpriteRenderer sr;

    public static FrameEffect Spawn(Sprite[] frames, Vector2 pos, float fps, int order, float scale = 1f)
    {
        var go = new GameObject("Effect");
        go.transform.position = pos;
        go.transform.localScale = new Vector3(scale, scale, 1);
        var fx = go.AddComponent<FrameEffect>();
        fx.sr = go.AddComponent<SpriteRenderer>();
        fx.sr.sortingOrder = order;
        fx.frames = frames;
        fx.frameTime = 1f / fps;
        fx.sr.sprite = frames[0];
        return fx;
    }

    void Update()
    {
        age += Time.deltaTime;
        int f = (int)(age / frameTime);
        if (f >= frames.Length) { Destroy(gameObject); return; }
        sr.sprite = frames[f];
    }
}

// Floating text such as "+1" or "LEVEL 2" that drifts up and blinks out.
public class Popup : MonoBehaviour
{
    float life, age, rise;
    PixelText text;

    public static void Spawn(string message, Vector2 pos, Color32 color, int scale, float life, float rise)
    {
        var t = PixelText.Create("Popup", null, pos, scale, color, new Vector2(0.5f, 0.5f), 30);
        t.Set(message);
        var p = t.gameObject.AddComponent<Popup>();
        p.text = t;
        p.life = life;
        p.rise = rise;
    }

    void Update()
    {
        age += Time.deltaTime;
        transform.position += Vector3.up * rise * Time.deltaTime;
        if (age > life * 0.6f) text.Renderer.enabled = ((int)(age * 16) % 2) == 0;
        if (age >= life) Destroy(gameObject);
    }
}
