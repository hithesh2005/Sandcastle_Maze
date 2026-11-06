// MazeAudio.cs - every sound effect and the ambient music loop are
// synthesised in code, so no audio files are needed.
using System.Collections.Generic;
using UnityEngine;

public static class MazeAudio
{
    static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

    public static AudioClip Get(string key)
    {
        if (clips.Count == 0) Init();
        AudioClip c;
        return clips.TryGetValue(key, out c) ? c : null;
    }

    // ADD NEW SOUNDS HERE, then call Play("yourKey") from the game
    static void Init()
    {
        clips["cob"] = Tone(880, 1320, 0.18f, 0.24f, 0f); // pearly seashell chime
        clips["coin"] = Tone(1040, 1560, 0.10f, 0.19f, 0f); // sand-dollar sparkle
        clips["hurt"] = Tone(220, 80, 0.40f, 0.30f, 0.3f);
        clips["gate"] = Tone(250, 720, 0.62f, 0.26f, 0.08f); // driftwood gate and shell bell
        clips["lose"] = Tone(320, 90, 0.90f, 0.30f, 0.2f);
        clips["click"] = Tone(900, 700, 0.06f, 0.18f, 0f);
        clips["hover"] = Tone(1400, 1500, 0.03f, 0.06f, 0f);
        clips["bark"] = Tone(420, 260, 0.18f, 0.22f, 0.25f); // crab click
        clips["caw"] = Tone(980, 620, 0.36f, 0.20f, 0.18f); // seagull call
        clips["win"] = Arp(new[] { 523f, 659f, 784f, 1047f }, 0.18f);
        clips["power"] = Arp(new[] { 440f, 660f, 880f }, 0.08f);
        clips["hint"] = Arp(new[] { 880f, 1175f }, 0.10f);
        clips["music"] = Music();
    }

    static AudioClip Tone(float f1, float f2, float dur, float vol, float rough)
    {
        int sr = 22050, len = (int)(sr * dur);
        var d = new float[len];
        float ph = 0;
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)sr;
            float f = Mathf.Lerp(f1, f2, t / dur);
            ph += 2f * Mathf.PI * f / sr;
            float s = Mathf.Sin(ph) + rough * 0.5f * Mathf.Sin(ph * 2.01f) + rough * 0.3f * Mathf.Sign(Mathf.Sin(ph * 0.5f));
            float env = Mathf.Min(1f, t * 60f) * (1f - t / dur);
            d[i] = s * vol * env;
        }
        var clip = AudioClip.Create("sfx", len, 1, sr, false);
        clip.SetData(d, 0);
        return clip;
    }

    static AudioClip Arp(float[] notes, float step)
    {
        int sr = 22050;
        float dur = step * notes.Length + 0.45f;
        int len = (int)(sr * dur);
        var d = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)sr, s = 0;
            for (int k = 0; k < notes.Length; k++)
            {
                float lt = t - k * step;
                if (lt < 0) continue;
                s += Mathf.Sin(2f * Mathf.PI * notes[k] * lt) * Mathf.Exp(-lt * 6f) * 0.22f;
            }
            d[i] = s;
        }
        var clip = AudioClip.Create("arp", len, 1, sr, false);
        clip.SetData(d, 0);
        return clip;
    }

    // 16-second bright coastal pad loop with soft surf-like swells
    static AudioClip Music()
    {
        int sr = 22050; float dur = 16f; int len = (int)(sr * dur);
        var d = new float[len];
        float[][] chords =
        {
            new[] { 261.6f, 329.6f, 392f }, new[] { 196f, 246.9f, 293.7f },
            new[] { 220f, 261.6f, 329.6f }, new[] { 174.6f, 220f, 261.6f }
        };
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)sr;
            int ci = Mathf.Min(3, (int)(t / 4f));
            float local = (t % 4f) / 4f;
            float env = Mathf.Sin(local * Mathf.PI);
            float s = 0;
            foreach (float f in chords[ci]) s += Mathf.Sin(2f * Mathf.PI * f * t);
            s += 0.8f * Mathf.Sin(2f * Mathf.PI * chords[ci][0] * 0.5f * t);
            float trem = 0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * 0.3f * t);
            float surf = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 0.11f * t), 3f);
            float shimmer = Mathf.Sin(2f * Mathf.PI * (640f + 35f * Mathf.Sin(t * .7f)) * t);
            d[i] = s * env * trem * 0.038f + surf * shimmer * 0.006f;
        }
        var clip = AudioClip.Create("music", len, 1, sr, false);
        clip.SetData(d, 0);
        return clip;
    }
}
