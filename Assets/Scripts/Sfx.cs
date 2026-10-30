using UnityEngine;

// Tiny chiptune sound effects synthesised at startup.
public class Sfx : MonoBehaviour
{
    const int Rate = 44100;

    AudioSource source;
    AudioClip coin, bomb, thud, start, gameOver, hurt;

    void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;

        coin = Tones("coin", new[] { 988f, 1319f }, new[] { 0.05f, 0.12f }, 0.25f);
        start = Tones("start", new[] { 523f, 659f, 784f, 1047f }, new[] { 0.07f, 0.07f, 0.07f, 0.15f }, 0.2f);
        gameOver = Tones("gameover", new[] { 392f, 330f, 262f, 196f }, new[] { 0.15f, 0.15f, 0.15f, 0.4f }, 0.22f);
        hurt = Tones("hurt", new[] { 220f, 165f }, new[] { 0.08f, 0.18f }, 0.25f);
        thud = Tones("thud", new[] { 110f }, new[] { 0.06f }, 0.12f);
        bomb = Noise("bomb", 0.6f, 0.5f);
    }

    static AudioClip Tones(string name, float[] freqs, float[] durations, float volume)
    {
        int total = 0;
        foreach (var d in durations) total += (int)(d * Rate);
        var data = new float[total];
        int i = 0;
        for (int n = 0; n < freqs.Length; n++)
        {
            int len = (int)(durations[n] * Rate);
            for (int s = 0; s < len; s++, i++)
            {
                float t = (float)s / Rate;
                float square = Mathf.Repeat(t * freqs[n], 1f) < 0.5f ? 1f : -1f;
                float env = 1f - (float)s / len * 0.7f;
                data[i] = square * env * volume;
            }
        }
        var clip = AudioClip.Create(name, total, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip Noise(string name, float duration, float volume)
    {
        int len = (int)(duration * Rate);
        var data = new float[len];
        var rng = new System.Random(7);
        float held = 0f;
        for (int s = 0; s < len; s++)
        {
            float k = (float)s / len;
            // Sample-and-hold noise that gets lower in pitch as it decays: a crunchy 8-bit boom.
            int hold = 1 + (int)(k * 24);
            if (s % hold == 0) held = (float)(rng.NextDouble() * 2 - 1);
            data[s] = held * (1f - k) * (1f - k) * volume;
        }
        var clip = AudioClip.Create(name, len, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    void Play(AudioClip clip, float pitch = 1f)
    {
        source.pitch = pitch;
        source.PlayOneShot(clip);
    }

    public void Coin(float pitch) => Play(coin, pitch);
    public void Bomb() => Play(bomb);
    public void Hurt() => Play(hurt);
    public void Thud() => Play(thud);
    public void Begin() => Play(start);
    public void GameOver() => Play(gameOver);
}
