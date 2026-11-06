// MazeVisuals.cs - procedural sand, sandstone, water, driftwood, UI textures and particles.
// Nothing here needs any imported asset: every texture is drawn in code.
using System.Collections.Generic;
using UnityEngine;

public static class MazeVisuals
{
    static Shader sh;
    static readonly Texture2D[] sandWallTextures = new Texture2D[4];
    static Texture2D beachSandTexture;
    static Texture2D beachWaterTexture;
    static Texture2D driftwoodTexture;
    static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

    static Shader LitShader()
    {
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        if (sh == null) sh = Shader.Find("HDRP/Lit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        return sh;
    }

    // Lit material that works in Built-in, URP and HDRP.
    public static Material Mat(Color c, float emit = 0f)
    {
        string key = c.ToString() + emit;
        Material m;
        if (cache.TryGetValue(key, out m) && m != null) return m;
        m = new Material(LitShader());
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.1f);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.1f);
        if (emit > 0f && m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * emit);
        }
        cache[key] = m;
        return m;
    }

    static Material TexturedMaterial(string key, Color tint, Texture2D tex, Vector2 tiling, float smoothness)
    {
        Material existing;
        if (cache.TryGetValue(key, out existing) && existing != null) return existing;

        var m = new Material(LitShader());
        m.color = tint;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", tint);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
        m.mainTexture = tex;
        m.mainTextureScale = tiling;
        if (m.HasProperty("_BaseMap"))
        {
            m.SetTexture("_BaseMap", tex);
            m.SetTextureScale("_BaseMap", tiling);
        }
        cache[key] = m;
        return m;
    }

    public static Material Unlit(Color c, Texture tex = null)
    {
        var s = Shader.Find("Sprites/Default");
        if (s == null) s = Shader.Find("Unlit/Color");
        var m = new Material(s);
        m.color = c;
        if (tex != null) m.mainTexture = tex;
        return m;
    }

    // ------------------------------------------------ procedural textures
    static Texture2D Make(int w, int h, System.Func<int, int, float> alpha)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = new Color(1, 1, 1, alpha(x, y));
        t.SetPixels(px); t.Apply();
        t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Bilinear;
        return t;
    }

    public static Texture2D RoundedRect(int size, int radius)
    {
        return Make(size, size, (x, y) =>
        {
            float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
            float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
            return Mathf.Clamp01(radius - d + 0.5f);
        });
    }

    public static Texture2D Circle(int s)
    {
        float r = s * 0.5f;
        return Make(s, s, (x, y) => Mathf.Clamp01(r - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) + 0.5f));
    }

    public static Texture2D Glow(int s)
    {
        float r = s * 0.5f;
        return Make(s, s, (x, y) =>
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
            return Mathf.Pow(Mathf.Clamp01(1f - d), 2f);
        });
    }

    public static Texture2D Vignette(int s)
    {
        float r = s * 0.5f;
        return Make(s, s, (x, y) =>
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
            return Mathf.SmoothStep(0f, 1f, (d - 0.45f) / 0.65f);
        });
    }

    public static Texture2D Heart(int s)
    {
        return Make(s, s, (x, y) =>
        {
            float u = (x / (float)(s - 1) - 0.5f) * 2.6f;
            float v = (y / (float)(s - 1) - 0.45f) * 2.8f;
            float a = u * u + v * v - 1f;
            float f = a * a * a - u * u * v * v * v;
            return f <= 0f ? 1f : 0f;
        });
    }

    public static Texture2D Arrow(int s)
    {
        float apex = s * 0.92f, bas = s * 0.12f, half = s * 0.36f, cx = s * 0.5f;
        return Make(s, s, (x, y) =>
        {
            if (y > apex || y < bas) return 0f;
            float w = (apex - y) / (apex - bas) * half;
            return Mathf.Abs(x + 0.5f - cx) <= w ? 1f : 0f;
        });
    }

    public static Texture2D Star(int s)
    {
        var v = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float a = Mathf.PI / 2f + i * Mathf.PI / 5f;
            float r = (i % 2 == 0 ? 0.48f : 0.20f) * s;
            v[i] = new Vector2(s * 0.5f + Mathf.Cos(a) * r, s * 0.52f + Mathf.Sin(a) * r);
        }
        return Make(s, s, (x, y) => Inside(v, x + 0.5f, y + 0.5f) ? 1f : 0f);
    }

    static bool Inside(Vector2[] v, float px, float py)
    {
        bool c = false;
        for (int i = 0, j = v.Length - 1; i < v.Length; j = i++)
            if (((v[i].y > py) != (v[j].y > py)) &&
                (px < (v[j].x - v[i].x) * (py - v[i].y) / (v[j].y - v[i].y) + v[i].x)) c = !c;
        return c;
    }

    // ------------------------------------------------------- beach materials
    // Sandstone wall variants: wind-strata, rippled blocks, shell speckles and masonry seams.
    public static Material SandWall(Color tint, int variant = 0)
    {
        int v = ((variant % sandWallTextures.Length) + sandWallTextures.Length) % sandWallTextures.Length;
        string key = "sand-wall|" + tint + "|" + v;
        return TexturedMaterial(key, tint, SandWallTexture(v), new Vector2(2.1f, 1.8f), 0.08f);
    }

    static Texture2D SandWallTexture(int style)
    {
        if (sandWallTextures[style] != null) return sandWallTextures[style];
        const int size = 128;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float coarse = Mathf.PerlinNoise(x * .035f + style * 3.1f, y * .035f + style * 2.7f);
            float fine = Mathf.PerlinNoise(x * .19f + 8.2f, y * .19f + 4.8f);
            float grain = (fine - .5f) * .10f;
            Color col = Color.Lerp(new Color(.84f, .73f, .57f), new Color(1f, .95f, .82f), .38f + coarse * .48f);
            if (style == 0)
            {
                float strata = Mathf.Abs(Mathf.Sin((y + Mathf.Sin(x * .045f) * 3f) * .17f));
                if (strata < .10f) col = Color.Lerp(col, new Color(.70f, .56f, .40f), .30f);
            }
            else if (style == 1)
            {
                float ripple = Mathf.Abs(Mathf.Sin(x * .14f + Mathf.Sin(y * .045f) * 2.2f));
                if (ripple < .10f) col = Color.Lerp(col, new Color(1f, .96f, .84f), .40f);
            }
            else if (style == 2)
            {
                if (fine > .79f) col = Color.Lerp(col, new Color(.94f, .76f, .56f), .60f);
                if (fine < .20f) col = Color.Lerp(col, new Color(.65f, .53f, .40f), .35f);
            }
            else
            {
                float seamX = Mathf.Abs(Mathf.Sin(x * .10f));
                float seamY = Mathf.Abs(Mathf.Sin(y * .14f));
                if (seamX < .075f || seamY < .065f)
                    col = Color.Lerp(col, new Color(.59f, .46f, .34f), .42f);
                else if (seamX < .13f || seamY < .12f)
                    col = Color.Lerp(col, new Color(1f, .97f, .86f), .22f);
            }
            col += new Color(grain, grain * .86f, grain * .66f);
            pixels[y * size + x] = new Color(Mathf.Clamp01(col.r), Mathf.Clamp01(col.g), Mathf.Clamp01(col.b), 1f);
        }
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
        tex.name = "Procedural_Sandcastle_Block_" + style;
        tex.SetPixels(pixels); tex.Apply(true, true);
        tex.wrapMode = TextureWrapMode.Repeat; tex.filterMode = FilterMode.Trilinear; tex.anisoLevel = 4;
        sandWallTextures[style] = tex;
        return tex;
    }

    // Fine beach sand with long, soft tidal ripples and scattered darker grains.
    public static Material BeachSand(Color tint, float repeats = 18f)
    {
        float tiles = Mathf.Max(1f, repeats);
        string key = "beach-sand|" + tint + "|" + tiles.ToString("0.0");
        return TexturedMaterial(key, tint, BeachSandTexture(), new Vector2(tiles, tiles), .04f);
    }

    static Texture2D BeachSandTexture()
    {
        if (beachSandTexture != null) return beachSandTexture;
        const int size = 128;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float broad = Mathf.PerlinNoise(x * .026f, y * .026f);
            float fine = Mathf.PerlinNoise(x * .22f + 6.4f, y * .22f + 3.7f);
            float ripple = .5f + .5f * Mathf.Sin(y * .20f + Mathf.Sin(x * .055f) * 2.5f);
            float shade = .80f + broad * .13f + ripple * .08f + (fine - .5f) * .08f;
            Color col = new Color(.93f, .79f, .58f) * shade;
            if (fine > .79f) col = Color.Lerp(col, new Color(.55f, .43f, .31f), .28f);
            pixels[y * size + x] = new Color(Mathf.Clamp01(col.r), Mathf.Clamp01(col.g), Mathf.Clamp01(col.b), 1f);
        }
        beachSandTexture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        beachSandTexture.name = "Procedural_Tidal_Beach_Sand";
        beachSandTexture.SetPixels(pixels); beachSandTexture.Apply(true, true);
        beachSandTexture.wrapMode = TextureWrapMode.Repeat; beachSandTexture.filterMode = FilterMode.Trilinear; beachSandTexture.anisoLevel = 4;
        return beachSandTexture;
    }

    // Turquoise water shader texture: overlapping wave bands and soft foam highlights.
    public static Material BeachWater(Color tint, float repeats = 8f)
    {
        float tiles = Mathf.Max(1f, repeats);
        string key = "beach-water|" + tint + "|" + tiles.ToString("0.0");
        return TexturedMaterial(key, tint, BeachWaterTexture(), new Vector2(tiles, tiles), .42f);
    }

    static Texture2D BeachWaterTexture()
    {
        if (beachWaterTexture != null) return beachWaterTexture;
        const int size = 128;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float n0 = Mathf.PerlinNoise(x * .035f, y * .035f);
            float n1 = Mathf.PerlinNoise(x * .09f + 3.2f, y * .09f + 8.1f);
            float wave = Mathf.Abs(Mathf.Sin((x * .12f + y * .055f) + Mathf.Sin(y * .045f) * 2.8f));
            Color col = Color.Lerp(new Color(.30f, .61f, .70f), new Color(.67f, .91f, .90f), .28f + n0 * .45f + n1 * .12f);
            if (wave < .12f) col = Color.Lerp(col, new Color(.88f, .99f, .94f), .34f);
            pixels[y * size + x] = new Color(Mathf.Clamp01(col.r), Mathf.Clamp01(col.g), Mathf.Clamp01(col.b), 1f);
        }
        beachWaterTexture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        beachWaterTexture.name = "Procedural_Turquoise_Sea_Waves";
        beachWaterTexture.SetPixels(pixels); beachWaterTexture.Apply(true, true);
        beachWaterTexture.wrapMode = TextureWrapMode.Repeat; beachWaterTexture.filterMode = FilterMode.Trilinear; beachWaterTexture.anisoLevel = 4;
        return beachWaterTexture;
    }

    // Sun-bleached driftwood with flowing grain for the castle gate and palms.
    public static Material Driftwood(Color tint)
    {
        return TexturedMaterial("beach-driftwood|" + tint, tint, DriftwoodTexture(), new Vector2(2.2f, 2f), .12f);
    }

    static Texture2D DriftwoodTexture()
    {
        if (driftwoodTexture != null) return driftwoodTexture;
        const int size = 128;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float n0 = Mathf.PerlinNoise(x * .035f, y * .035f);
            float grain = Mathf.Abs(Mathf.Sin(y * .12f + Mathf.Sin(x * .035f) * 3f));
            Color col = Color.Lerp(new Color(.75f, .62f, .46f), new Color(.99f, .89f, .72f), .42f + n0 * .43f);
            if (grain < .12f) col = Color.Lerp(col, new Color(.57f, .44f, .32f), .30f);
            pixels[y * size + x] = new Color(Mathf.Clamp01(col.r), Mathf.Clamp01(col.g), Mathf.Clamp01(col.b), 1f);
        }
        driftwoodTexture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        driftwoodTexture.name = "Procedural_SunBleached_Driftwood";
        driftwoodTexture.SetPixels(pixels); driftwoodTexture.Apply(true, true);
        driftwoodTexture.wrapMode = TextureWrapMode.Repeat; driftwoodTexture.filterMode = FilterMode.Trilinear; driftwoodTexture.anisoLevel = 4;
        return driftwoodTexture;
    }

    // ------------------------------------------------------- beach glints
    public static void MakeBeachGlints(Transform parent)
    {
        var go = new GameObject("SeaSprayGlints");
        go.transform.SetParent(parent, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.startLifetime = 5.5f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(.10f, .24f);
        main.startColor = new Color(.70f, .96f, 1f, 1f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 110;

        var em = ps.emission; em.rateOverTime = 12f;
        var sp = ps.shape; sp.shapeType = ParticleSystemShapeType.Box;
        sp.scale = new Vector3(34f, 2.8f, 34f); sp.position = new Vector3(0, 1.4f, 0);

        var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-.28f, .28f);
        vel.y = new ParticleSystem.MinMaxCurve(.02f, .24f);
        vel.z = new ParticleSystem.MinMaxCurve(-.28f, .28f);

        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(new Color(.82f, .97f, 1f), 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(.72f, .28f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sharedMaterial = Unlit(Color.white, Glow(32));
        ps.Play();
    }

}
