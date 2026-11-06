// =====================================================================
//  SANDCASTLE MAZE v2  -  CORE  (MaizeMazeGame.cs)
//  The game is split over 3 files that share one class:
//    MaizeMazeGame.cs          core: maze, player, enemies, flow, input
//    MaizeMazeGame.PowerUps.cs power-ups, hint path, objective helper
//    MaizeMazeGame.UI.cs       modern menu / HUD / settings / end screens
//  plus helpers: MazeVisuals.cs (textures, materials), MazeAudio.cs (sound)
//
//  Put all 5 files in Assets/MaizeMaze/ and press Play in ANY scene.
// =====================================================================
using System.Collections.Generic;
using UnityEngine;
#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public partial class MaizeMazeGame : MonoBehaviour
{
    // ------------------------------------------------------------ types
    enum Difficulty { Easy, Medium, Hard }
    enum State { Menu, Playing, Paused, Won, Lost }
    enum PK { Cob, Coin, Speed, Shield, Time, Heart }   // pickup kinds

    class Settings
    {
        public int cells, cobsNeeded, cobsTotal, coins, traps, dogs, crows, chaseSteps, powerups;
        public float time, dogSpeed, crowSpeed, crowRange, braid;
        public bool minimap, beam;
    }
    class Pickup { public Transform t; public PK k; public bool taken; public float phase; public Vector3 basePos; }
    class Dog { public Transform t; public Vector2Int cur, tgt, prev; public bool chasing; public float stun; }
    class Crow { public Transform t, wl, wr; public Vector3 home; public float ang, retreat, phase; public bool diving; }
    class ToastMsg { public string text; public Color col; public float life, max; }

    struct Theme
    {
        public string name;
        public Color sky, ambient, moon, ground, lantern, w1, w2, w3, water, driftwood, trim, coral;
        public float moonI;
    }

    // The game now uses one cohesive beach-fortress world theme.
    static readonly Theme[] Themes =
    {
        new Theme { name = "Sandcastle Shores",
            sky = new Color(.43f, .72f, .91f), ambient = new Color(.76f, .72f, .63f),
            moon = new Color(1f, .88f, .70f), moonI = 1.22f,
            ground = new Color(.98f, .91f, .78f), lantern = new Color(.35f, .88f, 1f),
            w1 = new Color(.98f, .90f, .75f), w2 = new Color(1f, .94f, .82f), w3 = new Color(.91f, .81f, .64f),
            water = new Color(.70f, .94f, .98f), driftwood = new Color(.94f, .87f, .75f),
            trim = new Color(1f, .91f, .72f), coral = new Color(.96f, .39f, .29f) }
    };

    // ------------------------------------------------------- constants
    const float CS = 3f;
    const float WALL_H = 2.8f;
    const float MM_H = 0.32f;
    const int MARKER_LAYER = 31;
    static readonly Vector2Int[] D4 =
        { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };

    // ---------------------------------------------- saved user settings
    float sens = 1f, volume = 0.8f, fovSet = 75f;
    bool musicOn = true, mapOn = true, bobOn = true;
    int themeIdx;

    // ------------------------------------------------------------ state
    State state = State.Menu;
    Difficulty diff = Difficulty.Easy;
    int extra;
    Settings cfg;

    int n; bool[,] wall; int[,] dist; float bfsTimer;

    GameObject levelRoot;
    Transform player, camT, marker, doorT, exitBeaconMarker;
    CharacterController cc;
    Camera cam, miniCam;
    Light lantern, moon;
    Renderer lampR;
    AudioSource sfx, musicSrc;

    float yaw, pitch, velY, stamina = 1f, bobT, shake, mazeMid, menuAngle, levelTime, endT, uiClock;
    bool exhausted;
    float timeLeft, invuln, flash, danger, dangerSmooth, gateToastCd, bannerT;
    int lives, cobs, score, levelStartScore, hits, coinsGot, powerGot, stars, timeBonus, flawlessBonus;
    bool newBest;
    string endMsg = "", bannerTitle = "", bannerSub = "";

    bool gateOpen; Vector3 gatePos;
    List<Pickup> pickups = new List<Pickup>();
    List<Dog> dogs = new List<Dog>();
    List<Crow> crows = new List<Crow>();
    List<Vector3> traps = new List<Vector3>();
    readonly List<ToastMsg> toasts = new List<ToastMsg>();

    static Material Mat(Color c, float e = 0f) { return MazeVisuals.Mat(c, e); }

    // ----------------------------------------------------- bootstrap
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (FindAll<MaizeMazeGame>().Length == 0)
            new GameObject("SandcastleMazeGame").AddComponent<MaizeMazeGame>();
    }

    static TObj[] FindAll<TObj>() where TObj : Object
    {
#if UNITY_2022_2_OR_NEWER
        return FindObjectsByType<TObj>(FindObjectsSortMode.None);
#else
        return FindObjectsOfType<TObj>();
#endif
    }

    void Awake()
    {
        foreach (var c in FindAll<Camera>()) c.enabled = false;
        foreach (var a in FindAll<AudioListener>()) a.enabled = false;
        foreach (var l in FindAll<Light>()) l.enabled = false;
        LoadPrefs();

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.fog = false;

        moon = new GameObject("BeachSunlight").AddComponent<Light>();
        moon.type = LightType.Directional; moon.shadows = LightShadows.None;
        moon.transform.rotation = Quaternion.Euler(48, -28, 0);

        var pg = new GameObject("Player");
        player = pg.transform;
        cc = pg.AddComponent<CharacterController>();
        cc.height = 1.8f; cc.radius = 0.35f; cc.center = new Vector3(0, 0.9f, 0); cc.stepOffset = 0.3f;

        var cg = new GameObject("BeachExplorerCamera");
        cg.tag = "MainCamera";
        camT = cg.transform; camT.SetParent(player, false); camT.localPosition = new Vector3(0, 1.6f, 0);
        cam = cg.AddComponent<Camera>();
        cam.fieldOfView = fovSet; cam.nearClipPlane = 0.1f; cam.farClipPlane = 160;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.cullingMask = ~(1 << MARKER_LAYER);
        cg.AddComponent<AudioListener>();
        sfx = cg.AddComponent<AudioSource>(); sfx.spatialBlend = 0;
        musicSrc = cg.AddComponent<AudioSource>(); musicSrc.spatialBlend = 0; musicSrc.loop = true;
        musicSrc.clip = MazeAudio.Get("music"); musicSrc.Play();

        lantern = new GameObject("ShellLantern").AddComponent<Light>();
        lantern.transform.SetParent(camT, false);
        lantern.type = LightType.Point; lantern.range = 15; lantern.intensity = 1.8f; lantern.shadows = LightShadows.None;

        var mg = new GameObject("ShoreMapCamera");
        miniCam = mg.AddComponent<Camera>();
        miniCam.orthographic = true; miniCam.clearFlags = CameraClearFlags.SolidColor;
        miniCam.backgroundColor = new Color(0.08f, 0.25f, 0.31f);
        miniCam.depth = 5; miniCam.enabled = false;
        mg.transform.rotation = Quaternion.Euler(90, 0, 0);

        marker = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
        Destroy(marker.GetComponent<Collider>());
        marker.localScale = Vector3.one * 3.6f;
        marker.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0.20f, 0.92f, 0.96f), 1.5f);
        marker.gameObject.layer = MARKER_LAYER;

        MazeVisuals.MakeBeachGlints(player);
        InitHint();
        ShowMenu();
    }

    // ------------------------------------------------------ prefs
    void LoadPrefs()
    {
        sens = PlayerPrefs.GetFloat("mm_sens", 1f);
        volume = PlayerPrefs.GetFloat("mm_vol", 0.8f);
        fovSet = PlayerPrefs.GetFloat("mm_fov", 75f);
        musicOn = PlayerPrefs.GetInt("mm_music", 1) == 1;
        mapOn = PlayerPrefs.GetInt("mm_map", 1) == 1;
        bobOn = PlayerPrefs.GetInt("mm_bob", 1) == 1;
        themeIdx = Mathf.Clamp(PlayerPrefs.GetInt("mm_theme", 0), 0, Themes.Length - 1);
    }

    void SavePrefs()
    {
        PlayerPrefs.SetFloat("mm_sens", sens); PlayerPrefs.SetFloat("mm_vol", volume); PlayerPrefs.SetFloat("mm_fov", fovSet);
        PlayerPrefs.SetInt("mm_music", musicOn ? 1 : 0); PlayerPrefs.SetInt("mm_map", mapOn ? 1 : 0);
        PlayerPrefs.SetInt("mm_bob", bobOn ? 1 : 0); PlayerPrefs.SetInt("mm_theme", themeIdx);
        PlayerPrefs.Save();
    }

    void ApplyTheme()
    {
        var t = Themes[themeIdx];
        cam.backgroundColor = t.sky;
        RenderSettings.ambientLight = t.ambient;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = t.sky;
        RenderSettings.fogStartDistance = 55f;
        RenderSettings.fogEndDistance = 175f;
        moon.color = t.moon; moon.intensity = t.moonI;
        moon.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
        lantern.color = t.lantern;
        if (miniCam != null) miniCam.backgroundColor = new Color(.08f, .25f, .31f);
    }

    // ---------------------------------------------------- difficulty
    Settings MakeSettings(Difficulty d, int ex)
    {
        var s = new Settings();
        switch (d)
        {
            case Difficulty.Easy:
                s.cells = 7; s.cobsNeeded = 3; s.cobsTotal = 5; s.coins = 10; s.traps = 3; s.dogs = 1; s.crows = 1; s.powerups = 4;
                s.time = 240; s.dogSpeed = 2.6f; s.crowSpeed = 3f; s.crowRange = 7; s.chaseSteps = 8;
                s.braid = 0.25f; s.minimap = true; s.beam = true; break;
            case Difficulty.Medium:
                s.cells = 11; s.cobsNeeded = 5; s.cobsTotal = 8; s.coins = 18; s.traps = 7; s.dogs = 2; s.crows = 2; s.powerups = 3;
                s.time = 210; s.dogSpeed = 3.8f; s.crowSpeed = 4f; s.crowRange = 9; s.chaseSteps = 12;
                s.braid = 0.10f; s.minimap = true; s.beam = true; break;
            default:
                s.cells = 15; s.cobsNeeded = 7; s.cobsTotal = 10; s.coins = 26; s.traps = 12; s.dogs = 3; s.crows = 3; s.powerups = 2;
                s.time = 260; s.dogSpeed = 4.8f; s.crowSpeed = 5f; s.crowRange = 11; s.chaseSteps = 16;
                s.braid = 0f; s.minimap = false; s.beam = false; break;
        }
        s.cells = Mathf.Min(s.cells + 2 * ex, 27);
        s.dogs += ex; s.crows += ex; s.traps += 2 * ex; s.time += 25 * ex;
        return s;
    }

    // ------------------------------------------------------- flow
    void ShowMenu()
    {
        state = State.Menu;
        diff = Difficulty.Easy; extra = 0;
        RebuildShowcase();
        toasts.Clear();
        if (hintLine) hintLine.enabled = false;
        Unlock();
        SetPage(Page.Main);
    }

    void RebuildShowcase()
    {
        cfg = MakeSettings(Difficulty.Easy, 0);
        BuildLevel();
        ComputeDist(new Vector2Int(-5, -5));   // nobody chases in the menu
    }

    void StartNewGame(Difficulty d) { diff = d; extra = 0; score = 0; StartLevel(); }

    void StartLevel()
    {
        cfg = MakeSettings(diff, extra);
        lives = 3; cobs = 0; hits = 0; coinsGot = 0; powerGot = 0;
        gateOpen = false; invuln = 0; flash = 0; danger = 0; dangerSmooth = 0;
        speedT = shieldT = hintT = hintCd = 0; stamina = 1; exhausted = false;
        timeLeft = cfg.time; levelStartScore = score; levelTime = 0;
        toasts.Clear();
        if (hintLine) hintLine.enabled = false;
        BuildLevel();
        state = State.Playing;
        Lock();
        bannerTitle = (extra > 0 ? "HARD +" + extra : diff.ToString().ToUpper()) + " SANDCASTLE";
        bannerSub = "Collect " + cfg.cobsNeeded + " seashells to open the castle gate";
        bannerT = 3.5f;
    }

    void NextLevel() { if (diff < Difficulty.Hard) diff++; else extra++; StartLevel(); }

    void Win()
    {
        state = State.Won;
        timeBonus = Mathf.RoundToInt(timeLeft) * 10;
        flawlessBonus = hits == 0 ? 200 : 0;
        score += timeBonus + flawlessBonus;
        float r = timeLeft / cfg.time;
        stars = r >= 0.5f ? 3 : (r >= 0.25f ? 2 : 1);
        string bk = "mm_best_" + diff, sk = "mm_stars_" + diff;
        newBest = score > PlayerPrefs.GetInt(bk, 0);
        if (newBest) PlayerPrefs.SetInt(bk, score);
        if (stars > PlayerPrefs.GetInt(sk, 0)) PlayerPrefs.SetInt(sk, stars);
        PlayerPrefs.Save();
        endT = 0; Play("win"); Unlock();
    }

    void Lose(string why)
    {
        if (state != State.Playing) return;
        state = State.Lost; endMsg = why; endT = 0; Play("lose"); Unlock();
    }

    void Unlock() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    void Lock() { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }

    void Toast(string text, Color col, float dur = 2.8f)
    {
        if (toasts.Count >= 4) toasts.RemoveAt(0);
        toasts.Add(new ToastMsg { text = text, col = col, life = dur, max = dur });
    }

    void Play(string key, float vol = 1f)
    {
        var c = MazeAudio.Get(key);
        if (c != null && sfx != null) sfx.PlayOneShot(c, vol * volume);
    }

    void Hurt(string why)
    {
        if (invuln > 0 || state != State.Playing) return;
        if (shieldT > 0)
        {
            shieldT = 0; invuln = 1.5f;
            Toast("Shield absorbed the hit!", PKColor(PK.Shield)); Play("power");
            return;
        }
        lives--; hits++; score = Mathf.Max(0, score - 50);
        invuln = 2f; flash = 1f; shake = 1f;
        Toast(why, cRed); Play("hurt");
        if (lives <= 0) Lose("You ran out of lives!");
    }

    // --------------------------------------------------- maze builder
    Vector3 T2W(int x, int y) { return new Vector3(x * CS, 0, y * CS); }
    Vector2Int W2T(Vector3 p) { return new Vector2Int(Mathf.RoundToInt(p.x / CS), Mathf.RoundToInt(p.z / CS)); }
    bool Open(int x, int y) { return x >= 1 && y >= 1 && x <= n - 2 && y <= n - 2 && !wall[x, y]; }
    float Flat(Vector3 a, Vector3 b) { a.y = 0; b.y = 0; return Vector3.Distance(a, b); }

    void GenerateMaze(int cells)
    {
        n = cells * 2 + 1;
        wall = new bool[n, n];
        for (int x = 0; x < n; x++) for (int y = 0; y < n; y++) wall[x, y] = true;

        var stack = new Stack<Vector2Int>();
        wall[1, 1] = false; stack.Push(new Vector2Int(1, 1));
        var dirs = new[] { new Vector2Int(2, 0), new Vector2Int(-2, 0), new Vector2Int(0, 2), new Vector2Int(0, -2) };
        while (stack.Count > 0)
        {
            var c = stack.Peek();
            var opts = new List<Vector2Int>();
            foreach (var d in dirs)
            {
                int nx = c.x + d.x, ny = c.y + d.y;
                if (nx > 0 && ny > 0 && nx < n - 1 && ny < n - 1 && wall[nx, ny]) opts.Add(d);
            }
            if (opts.Count == 0) { stack.Pop(); continue; }
            var dd = opts[Random.Range(0, opts.Count)];
            wall[c.x + dd.x / 2, c.y + dd.y / 2] = false;
            wall[c.x + dd.x, c.y + dd.y] = false;
            stack.Push(new Vector2Int(c.x + dd.x, c.y + dd.y));
        }

        int toRemove = Mathf.RoundToInt(cells * cells * cfg.braid), removed = 0;
        for (int i = 0; i < toRemove * 30 && removed < toRemove; i++)
        {
            int x = Random.Range(1, n - 1), y = Random.Range(1, n - 1);
            if (!wall[x, y]) continue;
            if (x % 2 == 1 && y % 2 == 0 && !wall[x, y - 1] && !wall[x, y + 1]) { wall[x, y] = false; removed++; }
            else if (x % 2 == 0 && y % 2 == 1 && !wall[x - 1, y] && !wall[x + 1, y]) { wall[x, y] = false; removed++; }
        }
        wall[n - 1, n - 2] = false;   // exit opening
    }

    void BuildLevel()
    {
        ApplyTheme();
        if (levelRoot != null) { levelRoot.SetActive(false); Destroy(levelRoot); }
        levelRoot = new GameObject("SandcastleIsland");
        var root = levelRoot.transform;
        exitBeaconMarker = null;
        pickups = new List<Pickup>(); dogs = new List<Dog>(); crows = new List<Crow>(); traps = new List<Vector3>();

        GenerateMaze(cfg.cells);
        var th = Themes[themeIdx];

        float mid = (n - 1) * 0.5f * CS; mazeMid = mid;
        float groundSize = (n + 8) * CS;

        // A shallow turquoise sea surrounds the sandy island beneath the maze.
        float waterSize = groundSize + 34f;
        Prim(PrimitiveType.Cube, root, new Vector3(mid, -0.78f, mid),
            new Vector3(waterSize, 0.46f, waterSize),
            MazeVisuals.BeachWater(th.water, Mathf.Max(5f, (n + 8) * 0.9f)), false);

        // Pale, rippled beach sand forms the full island floor.
        Prim(PrimitiveType.Cube, root, new Vector3(mid, -0.25f, mid),
            new Vector3(groundSize, 0.5f, groundSize),
            MazeVisuals.BeachSand(th.ground, Mathf.Max(8f, (n + 8) * 1.35f)));

        // Four procedural sand textures give the castle walls block and dune detail.
        var wm = new[]
        {
            MazeVisuals.SandWall(th.w1, 0), MazeVisuals.SandWall(th.w2, 1),
            MazeVisuals.SandWall(th.w3, 2), MazeVisuals.SandWall(th.w2, 3),
            MazeVisuals.SandWall(th.w1, 2), MazeVisuals.SandWall(th.w3, 0)
        };
        for (int x = 0; x < n; x++)
            for (int y = 0; y < n; y++)
            {
                if (!wall[x, y]) continue;
                float h = WALL_H * Random.Range(0.96f, 1.08f);
                Material blockMat = wm[Random.Range(0, wm.Length)];
                GameObject block = Prim(PrimitiveType.Cube, root,
                    new Vector3(x * CS, h * 0.5f, y * CS), new Vector3(CS, h, CS), blockMat);
                block.name = "SandcastleWallBlock";
                // A cap and a small merlon create a classic castle battlement silhouette.
                Prim(PrimitiveType.Cube, root, new Vector3(x * CS, h + 0.10f, y * CS),
                    new Vector3(CS * .94f, .20f, CS * .94f), blockMat, false);
                if ((x + y) % 2 == 0)
                    Prim(PrimitiveType.Cube, root, new Vector3(x * CS, h + .36f, y * CS),
                        new Vector3(CS * .42f, .38f, CS * .42f), blockMat, false);
            }

        BuildBeachDecor(root, n, th);

        // The exit is a driftwood portcullis set between sandstone towers.
        gatePos = T2W(n - 1, n - 2);
        var gate = new GameObject("CastleDrawbridgeGate").transform; gate.SetParent(root, false); gate.position = gatePos;
        var wood = MazeVisuals.Driftwood(th.driftwood);
        var stone = MazeVisuals.SandWall(th.trim, 1);
        Prim(PrimitiveType.Cube, gate, new Vector3(0, 1.5f, -1.45f), new Vector3(.62f, 3f, .52f), stone);
        Prim(PrimitiveType.Cube, gate, new Vector3(0, 1.5f, 1.45f), new Vector3(.62f, 3f, .52f), stone);
        Prim(PrimitiveType.Cube, gate, new Vector3(0, 3.12f, 0), new Vector3(.68f, .42f, 3.55f), stone, false);
        doorT = Prim(PrimitiveType.Cube, gate, new Vector3(0, 1.2f, 0),
            new Vector3(.32f, 2.4f, CS - .4f), wood).transform;
        // Bright shell emblem doubles as the locked / unlocked status light.
        lampR = Prim(PrimitiveType.Sphere, gate, new Vector3(0, 3.42f, 0),
            Vector3.one * .62f, Mat(new Color(.22f, .90f, .92f), 1.8f), false).GetComponent<Renderer>();
        Vector3 exitPos = T2W(n + 1, n - 2);
        Prim(PrimitiveType.Cylinder, root, exitPos + Vector3.up * .02f,
            new Vector3(4f, .03f, 4f), Mat(th.water, .75f), false);
        Prim(PrimitiveType.Cylinder, root, exitPos + Vector3.up * 1.6f,
            new Vector3(.12f, 1.6f, .12f), wood, false);
        Prim(PrimitiveType.Cube, root, exitPos + new Vector3(0, 2.8f, .45f),
            new Vector3(.05f, .6f, .9f), Mat(th.trim, .7f), false);
        if (cfg.beam)
            Prim(PrimitiveType.Cylinder, root, gatePos + Vector3.up * 10f,
                new Vector3(.22f, 10f, .22f), Mat(th.water, 1.8f), false);

        // Hard mode gets a tall, unmistakable exit beacon above the maze walls.
        // The beacon remains visible while the gate is locked; the HUD arrow
        // guides the player to shells first, then points to this exact exit.
        if (diff == Difficulty.Hard || extra > 0)
        {
            Material beaconBlue = Mat(new Color(.12f, .88f, 1f), 3.8f);
            Material beaconGold = Mat(new Color(1f, .66f, .16f), 3.2f);
            Prim(PrimitiveType.Cylinder, root, gatePos + Vector3.up * 15.5f,
                new Vector3(.34f, 15.5f, .34f), beaconBlue, false);
            Prim(PrimitiveType.Cylinder, root, gatePos + Vector3.up * 5.0f,
                new Vector3(1.05f, .10f, 1.05f), beaconGold, false);
            Prim(PrimitiveType.Cylinder, root, gatePos + Vector3.up * 29.0f,
                new Vector3(1.65f, .08f, 1.65f), beaconGold, false);

            var crown = new GameObject("HardModeExitBeaconCrown").transform;
            crown.SetParent(root, false);
            crown.position = gatePos + Vector3.up * 31.5f;
            exitBeaconMarker = crown;
            Prim(PrimitiveType.Sphere, crown, Vector3.zero,
                Vector3.one * 1.35f, beaconGold, false);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * .5f;
                Vector3 rayOffset = new Vector3(Mathf.Sin(angle) * .88f, 0f, Mathf.Cos(angle) * .88f);
                var ray = Prim(PrimitiveType.Cube, crown,
                    rayOffset, new Vector3(.34f, .16f, 1.55f), beaconGold, false);
                ray.transform.localRotation = Quaternion.Euler(0f, i * 90f, 24f);
            }
            Light beaconLight = crown.gameObject.AddComponent<Light>();
            beaconLight.type = LightType.Point;
            beaconLight.color = new Color(.25f, .90f, 1f);
            beaconLight.range = 26f;
            beaconLight.intensity = 3.2f;
            beaconLight.shadows = LightShadows.None;
        }

        // spawn tiles
        var free = new List<Vector2Int>();
        for (int x = 1; x <= n - 2; x++)
            for (int y = 1; y <= n - 2; y++)
                if (!wall[x, y] && !(x == 1 && y == 1) && !(x == n - 2 && y == n - 2)) free.Add(new Vector2Int(x, y));
        for (int i = free.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); var tmp = free[i]; free[i] = free[j]; free[j] = tmp; }

        Vector2Int Take(int minD)
        {
            for (int i = 0; i < free.Count; i++)
            {
                var t = free[i];
                if (Mathf.Abs(t.x - 1) + Mathf.Abs(t.y - 1) >= minD) { free.RemoveAt(i); return t; }
            }
            if (free.Count > 0) { var t = free[0]; free.RemoveAt(0); return t; }
            return new Vector2Int(1, 1);
        }

        for (int i = 0; i < cfg.cobsTotal; i++) { var t = Take(4); AddPickup(root, T2W(t.x, t.y) + Vector3.up * 1f, PK.Cob); }
        for (int i = 0; i < cfg.coins; i++) { var t = Take(2); AddPickup(root, T2W(t.x, t.y) + Vector3.up * 0.9f, PK.Coin); }
        SpawnPowerUps(root, Take);
        for (int i = 0; i < cfg.traps; i++) { var t = Take(4); var p = T2W(t.x, t.y); MakeTrap(root, p); traps.Add(p); }
        for (int i = 0; i < cfg.dogs; i++) { var t = Take(10); MakeDog(root, t); }
        for (int i = 0; i < cfg.crows; i++) { var t = Take(6); MakeCrow(root, T2W(t.x, t.y)); }

        miniCam.orthographicSize = (n + 3) * CS * 0.5f;
        miniCam.transform.position = new Vector3(mid + CS, 60, mid);

        cc.enabled = false;
        player.position = T2W(1, 1) + Vector3.up * 0.1f;
        cc.enabled = true;
        yaw = !wall[2, 1] ? 90f : 0f; pitch = 0; velY = 0;
        player.rotation = Quaternion.Euler(0, yaw, 0);
        camT.localRotation = Quaternion.identity;

        bfsTimer = 0; ComputeDist(W2T(player.position));
    }

    // Palm silhouettes and shell details make the island read as a beach,
    // without adding imported models or changing maze navigation.
    void BuildBeachDecor(Transform root, int mazeN, Theme th)
    {
        var trunkMat = MazeVisuals.Driftwood(th.driftwood);
        var leafMat = Mat(new Color(.18f, .49f, .39f));
        float edge = (mazeN - 1) * CS + 9f;
        Vector3[] sites = { new Vector3(-9f, 0f, -9f), new Vector3(edge, 0f, -9f),
                            new Vector3(-9f, 0f, edge), new Vector3(edge, 0f, edge) };
        for (int i = 0; i < sites.Length; i++)
        {
            var palm = new GameObject("BeachPalm").transform;
            palm.SetParent(root, false); palm.position = sites[i];
            Prim(PrimitiveType.Cylinder, palm, new Vector3(0f, 1.45f, 0f),
                new Vector3(.28f, 1.55f, .28f), trunkMat, false).transform.localRotation = Quaternion.Euler(0f, 0f, (i % 2 == 0 ? -7f : 7f));
            Prim(PrimitiveType.Sphere, palm, new Vector3(0f, 3.0f, 0f),
                new Vector3(.42f, .28f, .42f), leafMat, false);
            for (int leaf = 0; leaf < 6; leaf++)
            {
                float a = leaf * Mathf.PI / 3f + i * .4f;
                Vector3 dir = new Vector3(Mathf.Cos(a), .28f, Mathf.Sin(a));
                var frond = Prim(PrimitiveType.Capsule, palm, new Vector3(dir.x * .72f, 3.0f + dir.y * .32f, dir.z * .72f),
                    new Vector3(.13f, .92f, .16f), leafMat, false);
                frond.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
            }
            // A small shell at the foot of each palm.
            Prim(PrimitiveType.Sphere, palm, new Vector3(.55f, .13f, .25f),
                new Vector3(.38f, .14f, .30f), Mat(th.trim), false);
        }
    }

    // ---------------------------------------------- object factories
    void AddPickup(Transform root, Vector3 pos, PK k)
    {
        var p = new Pickup { k = k, phase = Random.value * 6.28f, basePos = pos };
        string displayName = k == PK.Cob ? "Seashell" : "SandDollar";
        var g = new GameObject(displayName).transform;
        g.SetParent(root, false); g.position = pos;
        if (k == PK.Cob)
        {
            // Fan-shaped shell: a pearly body with raised radial ribs.
            var shellMat = Mat(new Color(1f, .78f, .52f), .35f);
            var ridgeMat = Mat(new Color(1f, .91f, .76f), .45f);
            Prim(PrimitiveType.Sphere, g, Vector3.zero, new Vector3(.58f, .20f, .48f), shellMat, false);
            for (int rib = -2; rib <= 2; rib++)
            {
                var line = Prim(PrimitiveType.Cube, g, new Vector3(rib * .075f, .06f, 0f),
                    new Vector3(.035f, .035f, .40f - Mathf.Abs(rib) * .035f), ridgeMat, false);
                line.transform.localRotation = Quaternion.Euler(0f, rib * 7f, rib * 5f);
            }
            Prim(PrimitiveType.Sphere, g, new Vector3(0f, .05f, -.19f),
                new Vector3(.18f, .07f, .10f), Mat(new Color(.98f, .93f, .82f)), false);
        }
        else
        {
            // Flat, stamped sand-dollar token with a sea-glass centre.
            var disk = Prim(PrimitiveType.Cylinder, g, Vector3.zero,
                new Vector3(.42f, .055f, .42f), Mat(new Color(.90f, .78f, .53f), .25f), false);
            disk.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Prim(PrimitiveType.Sphere, g, Vector3.zero, new Vector3(.14f, .08f, .14f),
                Mat(new Color(.25f, .77f, .78f), .45f), false);
        }
        p.t = g; pickups.Add(p);
    }

    void MakeTrap(Transform root, Vector3 pos)
    {
        var g = new GameObject("SeaUrchinHazard").transform; g.SetParent(root, false); g.position = pos;
        Prim(PrimitiveType.Sphere, g, new Vector3(0f, .16f, 0f),
            new Vector3(.58f, .28f, .58f), Mat(Themes[themeIdx].coral, .25f), false);
        var spike = Mat(new Color(1f, .88f, .68f));
        for (int i = 0; i < 10; i++)
        {
            float a = i * Mathf.PI * .2f;
            Vector3 dir = new Vector3(Mathf.Cos(a), .55f + (i % 2) * .12f, Mathf.Sin(a));
            var s = Prim(PrimitiveType.Capsule, g, new Vector3(Mathf.Cos(a) * .34f, .30f, Mathf.Sin(a) * .34f),
                new Vector3(.07f, .34f, .07f), spike, false);
            s.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
        }
    }

    // The original ground-patrol AI now drives a small sand crab.
    void MakeDog(Transform root, Vector2Int tile)
    {
        var g = new GameObject("SandCrab").transform; g.SetParent(root, false); g.position = T2W(tile.x, tile.y);
        var shell = Mat(Themes[themeIdx].coral, .15f);
        var underside = Mat(new Color(.98f, .69f, .43f));
        var dark = Mat(new Color(.27f, .18f, .17f));
        Prim(PrimitiveType.Sphere, g, new Vector3(0f, .55f, 0f),
            new Vector3(.96f, .42f, .72f), shell, false);
        Prim(PrimitiveType.Sphere, g, new Vector3(0f, .39f, .02f),
            new Vector3(.66f, .22f, .52f), underside, false);
        for (int side = -1; side <= 1; side += 2)
        {
            for (int leg = 0; leg < 3; leg++)
            {
                float z = (leg - 1) * .34f;
                var legObj = Prim(PrimitiveType.Capsule, g, new Vector3(side * .48f, .30f, z),
                    new Vector3(.085f, .34f, .085f), shell, false);
                legObj.transform.localRotation = Quaternion.Euler(0f, 0f, side * 48f);
            }
            Prim(PrimitiveType.Sphere, g, new Vector3(side * .48f, .42f, .58f),
                new Vector3(.34f, .30f, .34f), shell, false);
            Prim(PrimitiveType.Sphere, g, new Vector3(side * .52f, .58f, .78f),
                Vector3.one * .10f, Mat(new Color(1f, .78f, .28f), 1.4f), false);
        }
        for (int eye = -1; eye <= 1; eye += 2)
        {
            Prim(PrimitiveType.Cylinder, g, new Vector3(eye * .22f, .82f, .42f),
                new Vector3(.055f, .16f, .055f), underside, false);
            Prim(PrimitiveType.Sphere, g, new Vector3(eye * .22f, .95f, .43f),
                Vector3.one * .085f, dark, false);
        }
        dogs.Add(new Dog { t = g, cur = tile, tgt = tile, prev = tile });
    }

    // The old crow patrol now uses a gliding white seagull model.
    void MakeCrow(Transform root, Vector3 home)
    {
        var g = new GameObject("Seagull").transform; g.SetParent(root, false); g.position = home + Vector3.up * 4f;
        var feather = Mat(new Color(.93f, .93f, .86f));
        var wingMat = Mat(new Color(.80f, .86f, .87f));
        var beak = Mat(new Color(1f, .59f, .20f), .15f);
        var dark = Mat(new Color(.12f, .18f, .20f));
        Prim(PrimitiveType.Sphere, g, Vector3.zero, new Vector3(.40f, .28f, .65f), feather, false);
        Prim(PrimitiveType.Sphere, g, new Vector3(0f, .10f, .34f), Vector3.one * .22f, feather, false);
        Prim(PrimitiveType.Cube, g, new Vector3(0f, .06f, .55f),
            new Vector3(.08f, .07f, .24f), beak, false);
        Prim(PrimitiveType.Sphere, g, new Vector3(-.105f, .16f, .43f), Vector3.one * .045f, dark, false);
        Prim(PrimitiveType.Sphere, g, new Vector3(.105f, .16f, .43f), Vector3.one * .045f, dark, false);
        var wl = new GameObject("WingL").transform; wl.SetParent(g, false); wl.localPosition = new Vector3(-.14f, .05f, 0f);
        var wr = new GameObject("WingR").transform; wr.SetParent(g, false); wr.localPosition = new Vector3(.14f, .05f, 0f);
        Prim(PrimitiveType.Cube, wl, new Vector3(-.34f, 0f, 0f),
            new Vector3(.68f, .045f, .36f), wingMat, false);
        Prim(PrimitiveType.Cube, wr, new Vector3(.34f, 0f, 0f),
            new Vector3(.68f, .045f, .36f), wingMat, false);
        crows.Add(new Crow { t = g, wl = wl, wr = wr, home = home, ang = Random.value * 6.28f, phase = Random.value * 6.28f });
    }

    GameObject Prim(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Material m, bool collider = true)
    {
        var g = GameObject.CreatePrimitive(type);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = localPos; g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = m;
        if (!collider) Destroy(g.GetComponent<Collider>());
        return g;
    }

    // ------------------------------------------------------- update
    void Update()
    {
        float dt = Time.deltaTime;
        uiClock += Time.unscaledDeltaTime;
        endT += dt;
        if (flash > 0) flash -= dt * 1.5f;
        if (bannerT > 0) bannerT -= dt;
        for (int i = toasts.Count - 1; i >= 0; i--) { toasts[i].life -= dt; if (toasts[i].life <= 0) toasts.RemoveAt(i); }

        musicSrc.volume = musicOn ? volume * 0.35f : 0f;

        bool showMap = cfg != null && cfg.minimap && mapOn && (state == State.Playing || state == State.Paused);
        miniCam.enabled = showMap;
        if (showMap)
        {
            Rect r = MiniRectPx();
            miniCam.rect = new Rect(r.x / Screen.width, (Screen.height - r.yMax) / Screen.height, r.width / Screen.width, r.height / Screen.height);
        }

        switch (state)
        {
            case State.Menu:
                UpdateMenuCamera(dt); UpdateAmbient(dt);
                // Escape always provides a reliable way back from menu sub-pages.
                if (GameInput.Escape() && page != Page.Main)
                {
                    SetPage(Page.Main);
                    return;
                }
                if (page == Page.Main)
                {
                    if (GameInput.MenuUp()) keyboardMenuChoice = (keyboardMenuChoice + 2) % 3;
                    if (GameInput.MenuDown()) keyboardMenuChoice = (keyboardMenuChoice + 1) % 3;
                    if (GameInput.Enter())
                    {
                        if (keyboardMenuChoice == 0) SetPage(Page.Select);
                        else if (keyboardMenuChoice == 1) SetPage(Page.Settings);
                        else Quit();
                        return;
                    }
                }
                else if (page == Page.Select)
                {
                    if (GameInput.MenuUp()) keyboardDifficultyChoice = (keyboardDifficultyChoice + 2) % 3;
                    if (GameInput.MenuDown()) keyboardDifficultyChoice = (keyboardDifficultyChoice + 1) % 3;
                    if (GameInput.Enter()) { StartNewGame((Difficulty)keyboardDifficultyChoice); return; }
                }
                break;
            case State.Playing:
                if (GameInput.Escape()) { state = State.Paused; pausePage = 0; Unlock(); return; }
                if (GameInput.MapKey() && cfg.minimap) mapOn = !mapOn;
                if (GameInput.HintKey()) TryHint();
                levelTime += dt;
                UpdatePlayer(dt);
                UpdateWorld(dt);
                break;
            case State.Paused:
                if (GameInput.Escape())
                {
                    if (pausePage == 1) { pausePage = 0; SavePrefs(); }
                    else { state = State.Playing; Lock(); }
                }
                else if (pausePage == 0 && GameInput.Enter())
                {
                    state = State.Playing; Lock();
                }
                break;
            case State.Won:
                if (GameInput.Enter()) NextLevel();
                else if (GameInput.Escape()) ShowMenu();
                break;
            case State.Lost:
                if (GameInput.Enter()) { score = levelStartScore; StartLevel(); }
                else if (GameInput.Escape()) ShowMenu();
                break;
        }
    }

    void UpdateMenuCamera(float dt)
    {
        menuAngle += dt * 0.12f;
        Vector3 c = new Vector3(mazeMid, 0, mazeMid);
        float r = mazeMid * 1.2f + 14f;
        Vector3 camPos = c + new Vector3(Mathf.Cos(menuAngle) * r, mazeMid * 0.9f + 12f, Mathf.Sin(menuAngle) * r);
        cc.enabled = false;
        player.rotation = Quaternion.identity;
        player.position = camPos - Vector3.up * 1.6f;
        camT.localPosition = new Vector3(0, 1.6f, 0);
        camT.LookAt(c);
        cam.fieldOfView = 60f;
        lantern.intensity = 0f;
    }

    void UpdateAmbient(float dt)
    {
        foreach (var p in pickups)
        {
            if (p.taken) continue;
            p.t.position = p.basePos + Vector3.up * Mathf.Sin(Time.time * 3f + p.phase) * 0.1f;
            p.t.Rotate(0, 120f * dt, 0, Space.World);
        }
        foreach (var d in dogs) UpdateDog(d, dt);
        foreach (var c in crows) UpdateCrow(c, dt);
    }

    void UpdatePlayer(float dt)
    {
        Vector2 look = GameInput.Look() * sens;
        yaw += look.x; pitch = Mathf.Clamp(pitch - look.y, -70f, 70f);
        player.rotation = Quaternion.Euler(0, yaw, 0);
        camT.localRotation = Quaternion.Euler(pitch, 0, 0);

        Vector2 mv = GameInput.Move();
        bool moving = mv.sqrMagnitude > 0.01f;
        Vector3 dir = player.right * mv.x + player.forward * mv.y;
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        // stamina-limited sprint
        if (exhausted && stamina > 0.3f) exhausted = false;
        bool sprint = GameInput.Sprint() && moving && !exhausted && stamina > 0f;
        if (sprint) { stamina -= 0.28f * dt; if (stamina <= 0f) { stamina = 0f; exhausted = true; } }
        else stamina = Mathf.Min(1f, stamina + 0.16f * dt);

        float sp = sprint ? 8.5f : 5.5f;
        if (speedT > 0) sp *= 1.4f;

        if (cc.isGrounded && velY < 0) velY = -1f;
        velY -= 20f * dt;
        cc.Move((dir * sp + Vector3.up * velY) * dt);

        // camera feel: FOV kick, head bob, damage shake
        float tf = fovSet + (sprint ? 8f : 0f) + (speedT > 0 ? 4f : 0f);
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, tf, 8f * dt);
        if (bobOn && moving && cc.isGrounded) bobT += dt * (sprint ? 13f : 9f);
        float bob = bobOn && moving ? Mathf.Sin(bobT) * 0.05f : 0f;
        shake = Mathf.MoveTowards(shake, 0f, dt * 2f);
        camT.localPosition = new Vector3(0, 1.6f + bob, 0) + Random.insideUnitSphere * shake * 0.08f;

        lantern.intensity = 1.8f + Mathf.PerlinNoise(Time.time * 5f, 0f) * 0.4f;
    }

    void UpdateWorld(float dt)
    {
        timeLeft -= dt;
        if (timeLeft <= 0) { timeLeft = 0; Lose("Time ran out!"); return; }
        if (invuln > 0) invuln -= dt;
        if (gateToastCd > 0) gateToastCd -= dt;
        UpdatePower(dt);

        Vector3 pp = player.position;
        marker.position = new Vector3(pp.x, 25f, pp.z);

        // Slowly turn and bob the Hard-mode beacon so it catches the eye.
        if (exitBeaconMarker != null)
        {
            exitBeaconMarker.Rotate(Vector3.up, 42f * dt, Space.World);
            Vector3 beaconPos = exitBeaconMarker.position;
            beaconPos.y = 31.5f + Mathf.Sin(Time.time * 2.1f) * .45f;
            exitBeaconMarker.position = beaconPos;
        }

        bfsTimer -= dt;
        if (bfsTimer <= 0) { bfsTimer = 0.25f; ComputeDist(W2T(pp)); if (hintT > 0) RefreshHint(); }

        foreach (var p in pickups)
        {
            if (p.taken) continue;
            p.t.position = p.basePos + Vector3.up * Mathf.Sin(Time.time * 3f + p.phase) * 0.1f;
            p.t.Rotate(0, 120f * dt, 0, Space.World);
            if (Flat(p.t.position, pp) < 1.1f) Collect(p);
        }

        foreach (var t in traps)
            if (Flat(t, pp) < 0.8f && invuln <= 0)
            {
                if (shieldT <= 0) timeLeft = Mathf.Max(1f, timeLeft - 10f);
                Hurt("Sea urchin!  -1 life  -10 sec");
            }

        float nearest = 999f;
        foreach (var d in dogs) { UpdateDog(d, dt); nearest = Mathf.Min(nearest, Flat(d.t.position, pp)); }
        foreach (var c in crows) { UpdateCrow(c, dt); nearest = Mathf.Min(nearest, Vector3.Distance(c.t.position, pp + Vector3.up)); }
        danger = Mathf.Clamp01(1f - (nearest - 3f) / 9f);
        dangerSmooth = Mathf.Lerp(dangerSmooth, danger, dt * 4f);

        if (!gateOpen && Flat(gatePos, pp) < 4f && gateToastCd <= 0)
        {
            Toast("Castle gate sealed - " + (cfg.cobsNeeded - cobs) + " more seashell(s) needed", cGold, 2f);
            gateToastCd = 3f;
        }
        if (gateOpen)
            doorT.localPosition = Vector3.MoveTowards(doorT.localPosition, new Vector3(0, 4.6f, 0), 3f * dt);

        if (gateOpen && pp.x > gatePos.x + 1f && Mathf.Abs(pp.z - gatePos.z) < CS) Win();
    }

    void OpenGate()
    {
        gateOpen = true;
        doorT.GetComponent<Collider>().enabled = false;
        lampR.sharedMaterial = Mat(new Color(.18f, .94f, 1f), 2f);
        Toast("Castle gate unlocked! Escape to the shore!", cTeal, 4f);
        Play("gate");
    }

    void ComputeDist(Vector2Int s)
    {
        if (dist == null || dist.GetLength(0) != n) dist = new int[n, n];
        for (int x = 0; x < n; x++) for (int y = 0; y < n; y++) dist[x, y] = 9999;
        if (!Open(s.x, s.y)) return;
        var q = new Queue<Vector2Int>();
        dist[s.x, s.y] = 0; q.Enqueue(s);
        while (q.Count > 0)
        {
            var c = q.Dequeue();
            foreach (var d in D4)
            {
                int nx = c.x + d.x, ny = c.y + d.y;
                if (Open(nx, ny) && dist[nx, ny] == 9999) { dist[nx, ny] = dist[c.x, c.y] + 1; q.Enqueue(new Vector2Int(nx, ny)); }
            }
        }
    }

    // ----------------------------------------------------- enemies
    void UpdateDog(Dog d, float dt)
    {
        if (d.stun > 0) { d.stun -= dt; return; }
        Vector3 target = T2W(d.tgt.x, d.tgt.y);
        Vector3 pos = d.t.position;
        float step = (d.chasing ? cfg.dogSpeed : cfg.dogSpeed * 0.55f) * dt;
        Vector3 to = target - pos; to.y = 0;
        if (to.magnitude <= step)
        {
            d.t.position = target;
            d.prev = d.cur; d.cur = d.tgt;
            PickNext(d);
        }
        else
        {
            d.t.position = pos + to.normalized * step;
            d.t.rotation = Quaternion.Slerp(d.t.rotation, Quaternion.LookRotation(to.normalized), 10f * dt);
        }
        if (state == State.Playing && Flat(d.t.position, player.position) < 1.0f && invuln <= 0)
        {
            Hurt("A sand crab pinched you!");
            d.stun = 1.5f;
        }
    }

    void PickNext(Dog d)
    {
        bool was = d.chasing;
        d.chasing = dist != null && dist[d.cur.x, d.cur.y] <= cfg.chaseSteps;
        if (d.chasing && !was && state == State.Playing) Play("bark", 0.7f);
        var nb = new List<Vector2Int>();
        foreach (var dd in D4)
        {
            var p = new Vector2Int(d.cur.x + dd.x, d.cur.y + dd.y);
            if (Open(p.x, p.y)) nb.Add(p);
        }
        if (nb.Count == 0) { d.tgt = d.cur; return; }
        if (d.chasing)
        {
            var best = nb[0];
            foreach (var p in nb) if (dist[p.x, p.y] < dist[best.x, best.y]) best = p;
            d.tgt = best;
        }
        else
        {
            if (nb.Count > 1) nb.Remove(d.prev);
            d.tgt = nb[Random.Range(0, nb.Count)];
        }
    }

    void UpdateCrow(Crow c, float dt)
    {
        Vector3 pp = player.position;
        Vector3 pos = c.t.position;
        Vector3 target; float speed; bool dive = false;
        if (c.retreat > 0)
        {
            c.retreat -= dt;
            target = c.home + Vector3.up * 5f; speed = cfg.crowSpeed;
        }
        else if (state == State.Playing && Flat(pos, pp) < cfg.crowRange)
        {
            target = pp + Vector3.up * 1.1f; speed = cfg.crowSpeed; dive = true;
        }
        else
        {
            c.ang += dt * 0.8f;
            target = c.home + new Vector3(Mathf.Cos(c.ang) * 4f, 4.2f + Mathf.Sin(Time.time * 2f + c.phase) * 0.3f, Mathf.Sin(c.ang) * 4f);
            speed = cfg.crowSpeed * 0.7f;
        }
        if (dive && !c.diving) Play("caw", 0.6f);
        c.diving = dive;

        Vector3 newPos = Vector3.MoveTowards(pos, target, speed * dt);
        Vector3 vel = newPos - pos;
        c.t.position = newPos;
        if (vel.sqrMagnitude > 1e-6f)
            c.t.rotation = Quaternion.Slerp(c.t.rotation, Quaternion.LookRotation(vel.normalized), 8f * dt);

        float flap = Mathf.Sin(Time.time * 16f + c.phase) * 35f;
        c.wl.localRotation = Quaternion.Euler(0, 0, flap);
        c.wr.localRotation = Quaternion.Euler(0, 0, -flap);

        if (state == State.Playing && c.retreat <= 0 && Vector3.Distance(newPos, pp + Vector3.up * 1.1f) < 1.0f)
        {
            Hurt("A seagull swooped at you!");
            c.retreat = 4f;
        }
    }
}

// ----------------------------------------------------------------------
//  Input wrapper: old Input Manager, new Input System, or both.
// ----------------------------------------------------------------------
static class GameInput
{
#if ENABLE_LEGACY_INPUT_MANAGER
    public static Vector2 Move() { return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")); }
    public static Vector2 Look() { return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 2f; }
    public static bool Sprint() { return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift); }
    public static bool Escape() { return Input.GetKeyDown(KeyCode.Escape); }
    public static bool HintKey() { return Input.GetKeyDown(KeyCode.H); }
    public static bool MapKey() { return Input.GetKeyDown(KeyCode.M); }
    public static Vector2 MousePos() { return Input.mousePosition; }
    public static bool MouseDown() { return Input.GetMouseButtonDown(0); }
    public static bool MouseHeld() { return Input.GetMouseButton(0); }
    public static bool Enter() { return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter); }
    public static bool MenuUp() { return Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W); }
    public static bool MenuDown() { return Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S); }
#elif ENABLE_INPUT_SYSTEM
    public static Vector2 Move()
    {
        var k = Keyboard.current; if (k == null) return Vector2.zero;
        float x = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0);
        float y = (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0);
        return new Vector2(x, y);
    }
    public static Vector2 Look() { return Mouse.current == null ? Vector2.zero : Mouse.current.delta.ReadValue() * 0.1f; }
    public static bool Sprint() { return Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed; }
    public static bool Escape() { return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame; }
    public static bool HintKey() { return Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame; }
    public static bool MapKey() { return Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame; }
    public static Vector2 MousePos() { return Mouse.current == null ? Vector2.zero : Mouse.current.position.ReadValue(); }
    public static bool MouseDown() { return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame; }
    public static bool MouseHeld() { return Mouse.current != null && Mouse.current.leftButton.isPressed; }
    public static bool Enter() { return Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame; }
    public static bool MenuUp() { return Keyboard.current != null && (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame); }
    public static bool MenuDown() { return Keyboard.current != null && (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame); }
#else
    public static Vector2 Move() { return Vector2.zero; }
    public static Vector2 Look() { return Vector2.zero; }
    public static bool Sprint() { return false; }
    public static bool Escape() { return false; }
    public static bool HintKey() { return false; }
    public static bool MapKey() { return false; }
    public static Vector2 MousePos() { return Vector2.zero; }
    public static bool MouseDown() { return false; }
    public static bool MouseHeld() { return false; }
    public static bool Enter() { return false; }
    public static bool MenuUp() { return false; }
    public static bool MenuDown() { return false; }
#endif
}
