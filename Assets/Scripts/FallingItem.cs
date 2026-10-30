using UnityEngine;

// A coin or bomb dropping straight down. GameManager drives it so it freezes outside of play.
public class FallingItem : MonoBehaviour
{
    public enum Kind { Coin, Bomb }

    public Kind kind;
    float speed, fps, age;
    Sprite[] frames;
    SpriteRenderer sr;

    public static FallingItem Spawn(Kind kind, Sprite[] frames, Vector2 pos, float speed)
    {
        var go = new GameObject(kind.ToString());
        go.transform.position = pos;
        var item = go.AddComponent<FallingItem>();
        item.sr = go.AddComponent<SpriteRenderer>();
        item.sr.sortingOrder = 5;
        item.kind = kind;
        item.frames = frames;
        item.speed = speed;
        item.fps = kind == Kind.Coin ? 10f : 12f;
        item.age = Random.value;
        item.sr.sprite = frames[0];
        return item;
    }

    public void Tick(float dt)
    {
        age += dt;
        transform.position += Vector3.down * speed * dt;
        sr.sprite = frames[(int)(age * fps) % frames.Length];
    }

    // Slightly smaller than the sprite so near-misses feel fair.
    public Rect Bounds
    {
        get
        {
            float inset = (kind == Kind.Coin ? 1f : 2f) / PixelArt.PPU;
            var b = sr.bounds;
            return new Rect(b.min.x + inset, b.min.y + inset, b.size.x - inset * 2, b.size.y - inset * 2);
        }
    }
}
