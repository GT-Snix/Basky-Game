using UnityEngine;
using UnityEngine.InputSystem;

// The player's basket. Moves with arrows / A-D, or follows the mouse / trackpad.
public class Basket : MonoBehaviour
{
    public float maxSpeed = 16f;
    public float acceleration = 140f;

    SpriteRenderer sr;
    float posX, vx, stunTimer, invulnTimer, squashTimer;
    bool mouseMode;

    static float HalfWidth => PixelArt.BasketW / 2f / PixelArt.PPU;
    static float Height => (float)PixelArt.BasketH / PixelArt.PPU;
    float Top => transform.position.y + Height;

    public bool Invulnerable => invulnTimer > 0f;

    // The opening of the basket: coins count only when they fall into this strip.
    public Rect CatchRect => new Rect(posX - HalfWidth + 0.15f, Top - 0.4f, (HalfWidth - 0.15f) * 2f, 0.55f);
    public Rect BodyRect => new Rect(posX - HalfWidth, transform.position.y, HalfWidth * 2f, Height);
    public Vector2 TopCenter => new Vector2(posX, Top);
    public float X => posX;

    void Awake() => EnsureRenderer();

    void EnsureRenderer()
    {
        if (sr != null) return;
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = PixelArt.Basket();
        sr.sortingOrder = 10;
    }

    public void ResetState(float x)
    {
        EnsureRenderer();
        vx = stunTimer = invulnTimer = squashTimer = 0f;
        posX = x;
        transform.position = new Vector3(x, transform.position.y, 0);
        sr.enabled = true;
        sr.color = Color.white;
    }

    public void Stun(float duration) => stunTimer = duration;
    public void MakeInvulnerable(float duration) => invulnTimer = duration;
    public void Squash() => squashTimer = 0.15f;

    public void Tick(Camera cam, bool controllable, float dt)
    {
        float dir = 0f;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) dir -= 1f;
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) dir += 1f;
        }
        var mouse = Mouse.current;
        if (dir != 0f) mouseMode = false;
        else if (mouse != null && mouse.delta.ReadValue().sqrMagnitude > 1f) mouseMode = true;

        stunTimer -= dt;
        invulnTimer -= dt;
        squashTimer -= dt;

        float x = posX;
        if (!controllable || stunTimer > 0f)
        {
            vx = 0f;
        }
        else if (mouseMode && mouse != null)
        {
            float target = cam.ScreenToWorldPoint(mouse.position.ReadValue()).x;
            x = Mathf.MoveTowards(x, target, maxSpeed * 1.5f * dt);
        }
        else
        {
            vx = Mathf.MoveTowards(vx, dir * maxSpeed, acceleration * dt);
            x += vx * dt;
        }

        float limit = GameManager.HalfWidth - HalfWidth;
        if (Mathf.Abs(x) > limit) { x = Mathf.Sign(x) * limit; vx = 0f; }
        posX = x;

        // Feedback: squash on catch, grey jitter while stunned, blink while invulnerable.
        transform.localScale = squashTimer > 0f ? new Vector3(1.125f, 0.875f, 1f) : Vector3.one;
        bool stunned = stunTimer > 0f;
        sr.color = stunned ? new Color(0.6f, 0.6f, 0.7f) : Color.white;
        float jitter = stunned ? (((int)(stunTimer * 30) % 2 == 0) ? 1f : -1f) / PixelArt.PPU : 0f;
        transform.position = new Vector3(posX + jitter, transform.position.y, 0);
        sr.enabled = !Invulnerable || stunned || ((int)(invulnTimer * 12) % 2 == 0);
    }
}
