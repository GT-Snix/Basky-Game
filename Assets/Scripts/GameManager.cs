using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

// Builds the whole Coin Catcher game at runtime and runs its rules.
// Drop this on an empty GameObject in a scene that has a Main Camera and a Global Light 2D.
public class GameManager : MonoBehaviour
{
    enum State { Title, Playing, Dying, GameOver }

    [Header("Rules")]
    [SerializeField] int startingLives = 3;
    [SerializeField] bool missedCoinCostsLife = false;
    [SerializeField] int coinsPerLevel = 15;
    [SerializeField] float bombBlastRadius = 1.6f;

    [Header("Difficulty")]
    [SerializeField] float baseSpawnInterval = 1.0f;
    [SerializeField] float minSpawnInterval = 0.3f;
    [SerializeField] float baseFallSpeed = 3f;
    [SerializeField] float maxFallSpeed = 9f;
    [SerializeField] float baseBombChance = 0.15f;
    [SerializeField] float maxBombChance = 0.35f;

    // Play area in world units: 320x200 pixels at 16 pixels per unit (16:10, MacBook-shaped).
    public const float HalfWidth = 10f, HalfHeight = 6.25f, FloorY = -5.25f;
    const float SpawnY = HalfHeight + 1f;
    const string BestKey = "CoinCatcher.Best";

    Camera cam;
    Vector3 camHome;
    Sfx sfx;
    Basket basket;
    readonly List<FallingItem> items = new List<FallingItem>();
    Sprite[] coinFrames, bombFrames, explosionFrames, dustFrames;
    Sprite heartFull, heartEmpty;

    PixelText scoreText, bestText, levelText;
    SpriteRenderer[] hearts;
    GameObject overlay;
    PixelText overlayTitle, overlayPrompt;
    PixelText[] overlayLines;
    SpriteRenderer flash;

    State state;
    int score, best, lives, level, streak;
    float spawnTimer, stateTimer, shakeTime, shakeStrength, flashAlpha;
    float bombRainTimer, bombRainTick;
    int bombRainLeft;

    float SpawnInterval => Mathf.Max(minSpawnInterval, baseSpawnInterval * Mathf.Pow(0.9f, level - 1));
    float FallSpeed => Mathf.Min(maxFallSpeed, baseFallSpeed + 0.5f * (level - 1));
    float BombChance => Mathf.Min(maxBombChance, baseBombChance + 0.03f * (level - 1));

    void Awake()
    {
        Application.targetFrameRate = 120;
        best = PlayerPrefs.GetInt(BestKey, 0);

        coinFrames = PixelArt.CoinFrames();
        bombFrames = PixelArt.BombFrames();
        explosionFrames = PixelArt.ExplosionFrames();
        dustFrames = new[] { explosionFrames[4], explosionFrames[5] };
        heartFull = PixelArt.Heart(true);
        heartEmpty = PixelArt.Heart(false);

        sfx = gameObject.AddComponent<Sfx>();
        SetupCamera();
        BuildWorld();
        BuildHud();
        BuildOverlay();

        basket = new GameObject("Basket").AddComponent<Basket>();
        basket.transform.position = new Vector3(0, FloorY, 0);
        basket.ResetState(0);

        ShowTitle();
    }

    void SetupCamera()
    {
        cam = Camera.main;
        if (cam == null) cam = new GameObject("Main Camera", typeof(Camera)) { tag = "MainCamera" }.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = HalfHeight;
        cam.transform.position = camHome = new Vector3(0, 0, -10);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = (Color)PixelArt.C('n');

        var ppc = cam.GetComponent<PixelPerfectCamera>();
        if (ppc == null) ppc = cam.gameObject.AddComponent<PixelPerfectCamera>();
        ppc.assetsPPU = PixelArt.PPU;
        ppc.refResolutionX = 320;
        ppc.refResolutionY = 200;
        ppc.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
        ppc.cropFrame = PixelPerfectCamera.CropFrame.None;
    }

    SpriteRenderer MakeSprite(string name, Sprite sprite, Vector2 pos, int order, Transform parent = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return sr;
    }

    void BuildWorld()
    {
        var world = new GameObject("World").transform;

        // Background is oversized so wider or taller windows never show an empty edge.
        MakeSprite("Background", PixelArt.Background(480, 300, 16), new Vector2(0, FloorY - 1f), -100, world);

        var grass = PixelArt.GroundTile(true);
        var dirt = PixelArt.GroundTile(false);
        for (int x = -16; x <= 16; x++)
            for (int row = 0; row < 4; row++)
                MakeSprite("Ground", row == 0 ? grass : dirt, new Vector2(x + 0.5f, FloorY - row), 0, world);

        flash = MakeSprite("Flash", PixelArt.Solid(PixelArt.C('w')), Vector2.zero, 90, world);
        flash.transform.localScale = new Vector3(200, 200, 1);
        flash.color = new Color(1, 1, 1, 0);
    }

    void BuildHud()
    {
        var hud = new GameObject("HUD").transform;
        Color32 white = PixelArt.C('w');
        scoreText = PixelText.Create("Score", hud, new Vector2(-9.75f, 6f), 2, PixelArt.C('y'), new Vector2(0, 1), 50);
        bestText = PixelText.Create("Best", hud, new Vector2(-9.75f, 5.0f), 1, white, new Vector2(0, 1), 50);
        levelText = PixelText.Create("Level", hud, new Vector2(0, 6f), 1, white, new Vector2(0.5f, 1), 50);

        hearts = new SpriteRenderer[startingLives];
        for (int i = 0; i < startingLives; i++)
        {
            hearts[i] = MakeSprite("Heart", heartFull, PixelText.Snap(new Vector2(9.25f - i * 1.0f, 5.5f)), 50, hud);
            hearts[i].transform.localScale = new Vector3(2, 2, 1);
        }
    }

    void BuildOverlay()
    {
        overlay = new GameObject("Overlay");
        var dim = MakeSprite("Dim", PixelArt.Solid(PixelArt.C('k')), Vector2.zero, 60, overlay.transform);
        dim.transform.localScale = new Vector3(200, 200, 1);
        dim.color = new Color(1, 1, 1, 0.7f);

        var center = new Vector2(0.5f, 0.5f);
        overlayTitle = PixelText.Create("Title", overlay.transform, new Vector2(0, 2.75f), 3, PixelArt.C('y'), center, 70);
        overlayLines = new PixelText[3];
        for (int i = 0; i < overlayLines.Length; i++)
            overlayLines[i] = PixelText.Create("Line", overlay.transform, new Vector2(0, 0.9f - i * 0.75f), 1, PixelArt.C('w'), center, 70);
        overlayPrompt = PixelText.Create("Prompt", overlay.transform, new Vector2(0, -2.75f), 2, PixelArt.C('C'), center, 70);
    }

    // ---------- States ----------

    void ShowTitle()
    {
        state = State.Title;
        overlay.SetActive(true);
        overlayTitle.Set("COIN CATCHER", PixelArt.C('y'));
        overlayLines[0].Set("CATCH THE COINS - DODGE THE BOMBS!");
        overlayLines[1].Set("ARROWS / A D OR MOUSE TO MOVE");
        overlayLines[2].Set("YOU HAVE " + startingLives + " LIVES");
        overlayPrompt.Set("PRESS SPACE TO START");
        RefreshHud();
    }

    void StartGame()
    {
        ClearItems(false);
        score = streak = 0;
        level = 1;
        lives = startingLives;
        spawnTimer = 0.6f;
        bombRainTimer = Random.Range(18f, 26f);
        bombRainLeft = 0;
        basket.ResetState(0);
        overlay.SetActive(false);
        state = State.Playing;
        sfx.Begin();
        RefreshHud();
    }

    void ShowGameOver()
    {
        state = State.GameOver;
        stateTimer = 0.6f; // short lockout so a held key doesn't instantly restart
        bool newBest = score > best;
        if (newBest)
        {
            best = score;
            PlayerPrefs.SetInt(BestKey, best);
            PlayerPrefs.Save();
        }
        overlay.SetActive(true);
        overlayTitle.Set("GAME OVER", PixelArt.C('O'));
        overlayLines[0].Set("SCORE " + score);
        overlayLines[1].Set(newBest ? "NEW BEST!" : "BEST " + best);
        overlayLines[2].Set("REACHED LEVEL " + level);
        overlayPrompt.Set("PRESS SPACE TO RETRY");
        RefreshHud();
    }

    // ---------- Loop ----------

    void Update()
    {
        float dt = Time.deltaTime;
        bool start = StartPressed();

        switch (state)
        {
            case State.Title:
                basket.Tick(cam, true, dt);
                if (start) StartGame();
                break;
            case State.Playing:
                basket.Tick(cam, true, dt);
                SpawnStep(dt);
                ItemsStep(dt);
                break;
            case State.Dying:
                basket.Tick(cam, false, dt);
                stateTimer -= dt;
                if (stateTimer <= 0f) ShowGameOver();
                break;
            case State.GameOver:
                basket.Tick(cam, true, dt);
                stateTimer -= dt;
                if (start && stateTimer <= 0f) StartGame();
                break;
        }

        if (overlay.activeSelf) overlayPrompt.Renderer.enabled = (int)(Time.time * 2.5f) % 2 == 0;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Application.Quit();

        EffectsStep(dt);
    }

    static bool StartPressed()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        return (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
            || (mouse != null && mouse.leftButton.wasPressedThisFrame);
    }

    void SpawnStep(float dt)
    {
        spawnTimer -= dt;
        if (spawnTimer <= 0f)
        {
            spawnTimer = SpawnInterval * Random.Range(0.75f, 1.25f);
            SpawnItem(Random.value < BombChance ? FallingItem.Kind.Bomb : FallingItem.Kind.Coin);
        }

        // From level 3, a "bomb rain" periodically drops a burst of bombs to shake things up.
        if (level >= 3 && bombRainLeft == 0)
        {
            bombRainTimer -= dt;
            if (bombRainTimer <= 0f)
            {
                bombRainTimer = Random.Range(18f, 28f);
                bombRainLeft = 4 + level / 2;
                bombRainTick = 1.2f;
                Popup.Spawn("BOMB RAIN!", new Vector2(0, 2f), PixelArt.C('O'), 3, 1.4f, 0.3f);
                sfx.Hurt();
            }
        }
        if (bombRainLeft > 0)
        {
            bombRainTick -= dt;
            if (bombRainTick <= 0f)
            {
                SpawnItem(FallingItem.Kind.Bomb);
                bombRainLeft--;
                bombRainTick = 0.3f;
            }
        }
    }

    void SpawnItem(FallingItem.Kind kind)
    {
        float x = Random.Range(-HalfWidth + 0.75f, HalfWidth - 0.75f);
        var frames = kind == FallingItem.Kind.Coin ? coinFrames : bombFrames;
        items.Add(FallingItem.Spawn(kind, frames, new Vector2(x, SpawnY), FallSpeed * Random.Range(0.9f, 1.15f)));
    }

    void ItemsStep(float dt)
    {
        bool hit = false;
        for (int i = items.Count - 1; i >= 0; i--)
        {
            var item = items[i];
            item.Tick(dt);
            Rect r = item.Bounds;
            Vector2 pos = item.transform.position;

            if (item.kind == FallingItem.Kind.Coin)
            {
                if (r.Overlaps(basket.CatchRect)) CatchCoin();
                else if (r.yMin <= FloorY) MissCoin(pos);
                else continue;
            }
            else
            {
                if (r.Overlaps(basket.BodyRect))
                {
                    Explode(pos, 1f);
                    hit = true;
                }
                else if (r.yMin <= FloorY)
                {
                    // Bombs that land still explode, and can catch a basket parked next to them.
                    Explode(new Vector2(pos.x, FloorY + 0.5f), 1f);
                    if (Mathf.Abs(basket.X - pos.x) < bombBlastRadius) hit = true;
                    else Shake(0.2f, 0.1f);
                }
                else continue;
            }
            Destroy(item.gameObject);
            items.RemoveAt(i);
        }
        if (hit) BombHit();
    }

    void CatchCoin()
    {
        score++;
        streak++;
        sfx.Coin(1f + Mathf.Min(streak, 12) * 0.04f);
        basket.Squash();
        Popup.Spawn("+1", basket.TopCenter + Vector2.up * 0.5f, PixelArt.C('y'), 1, 0.6f, 1.5f);

        int newLevel = 1 + score / coinsPerLevel;
        if (newLevel != level)
        {
            level = newLevel;
            Popup.Spawn("LEVEL " + level, new Vector2(0, 1.5f), PixelArt.C('g'), 3, 1.5f, 0.4f);
            sfx.Begin();
        }
        RefreshHud();
    }

    void MissCoin(Vector2 pos)
    {
        streak = 0;
        sfx.Thud();
        FrameEffect.Spawn(dustFrames, new Vector2(pos.x, FloorY + 0.25f), 12f, 20, 0.5f);
        if (missedCoinCostsLife) LoseLife();
    }

    void Explode(Vector2 pos, float scale)
    {
        sfx.Bomb();
        FrameEffect.Spawn(explosionFrames, pos, 16f, 20, scale);
    }

    // The bomb's big disruption: lose a life, wipe the sky, stun the basket.
    void BombHit()
    {
        if (basket.Invulnerable) return;
        Shake(0.45f, 0.3f);
        flashAlpha = 0.8f;
        ClearItems(true);
        bombRainLeft = 0;
        spawnTimer = 1f;
        basket.Stun(0.7f);
        basket.MakeInvulnerable(1.8f);
        LoseLife();
    }

    void LoseLife()
    {
        if (state != State.Playing) return;
        lives = Mathf.Max(0, lives - 1);
        streak = 0;
        sfx.Hurt();
        RefreshHud();
        if (lives == 0)
        {
            state = State.Dying;
            stateTimer = 1.2f;
            ClearItems(true);
            sfx.GameOver();
        }
    }

    void ClearItems(bool withPoof)
    {
        foreach (var item in items)
        {
            if (withPoof) FrameEffect.Spawn(dustFrames, item.transform.position, 12f, 20, 0.6f);
            Destroy(item.gameObject);
        }
        items.Clear();
    }

    void RefreshHud()
    {
        scoreText.Set("SCORE " + score);
        bestText.Set("BEST " + Mathf.Max(best, score));
        levelText.Set(state == State.Title ? "" : "LEVEL " + level);
        int shown = state == State.Title ? startingLives : lives;
        for (int i = 0; i < hearts.Length; i++)
            hearts[i].sprite = i < shown ? heartFull : heartEmpty;
    }

    // ---------- Juice ----------

    void Shake(float duration, float strength)
    {
        if (strength < shakeStrength && shakeTime > 0f) return;
        shakeTime = duration;
        shakeStrength = strength;
    }

    void EffectsStep(float dt)
    {
        Vector3 offset = Vector3.zero;
        if (shakeTime > 0f)
        {
            shakeTime -= dt;
            Vector2 r = Random.insideUnitCircle * shakeStrength;
            // Snap the shake to whole pixels so the art never smears.
            offset = new Vector3(Mathf.Round(r.x * PixelArt.PPU), Mathf.Round(r.y * PixelArt.PPU), 0) / PixelArt.PPU;
        }
        cam.transform.position = camHome + offset;

        flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, dt * 3f);
        flash.color = new Color(1, 1, 1, flashAlpha);
    }
}
