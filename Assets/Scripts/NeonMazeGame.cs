using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class NeonMazeGame : MonoBehaviour
{
    public static NeonMazeGame Instance;
    public Text scoreText, timerText, messageText;
    public GameObject endPanel;
    public Text endTitle, endDetails;
    public Button primaryActionButton;
    public Text primaryActionText;

    public int totalCoins = 0;
    public int coinsCollected = 0;
    public int score = 0;
    public static int currentLevel = 1;
    public static int totalScoreAcrossLevels = 0;
    public int maxLevel = 5;
    public float timeLimit = 180f;
    public bool hasKey = false;

    public Transform keyTransform;
    public NeonExit exitGate;

    private float timeLeft;
    private bool ended;
    private bool wonThisLevel;
    private float messageResetTimer;
    private string defaultControlsHint = "WASD: Move  |  [C] Dog/Human  |  [V] Camera Angle  |  [Q/E] Rotate  |  [R] Restart";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureMazeExistsOnPlay()
    {
        if (UnityEngine.Object.FindFirstObjectByType<NeonMazeGame>() == null &&
            UnityEngine.Object.FindFirstObjectByType<NeonPlayer>() == null)
        {
            BuildCurrentLevel();
        }
    }

    void Awake()
    {
        Instance = this;
        timeLimit = 120f + (currentLevel * 30f);
        timeLeft = timeLimit;
    }

    void Start()
    {
        if (!exitGate) exitGate = UnityEngine.Object.FindFirstObjectByType<NeonExit>();
        if (!keyTransform)
        {
            foreach (NeonCoin c in UnityEngine.Object.FindObjectsByType<NeonCoin>(FindObjectsSortMode.None))
            {
                if (c != null && c.itemKind == NeonCoin.ItemKind.GoldenKey)
                {
                    keyTransform = c.transform;
                    break;
                }
            }
        }
        // Hide the UGUI endPanel during gameplay; we also provide an OnGUI button so clicks never fail
        if (endPanel) endPanel.SetActive(false);
        Refresh();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartCurrentLevel();
            return;
        }

        if (ended)
        {
            // Allow pressing Enter or Space to trigger Next Level / Play Again immediately
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            {
                OnPrimaryButtonClicked();
            }
            return;
        }

        timeLeft -= Time.deltaTime;
        if (timerText) timerText.text = "TIME: " + Mathf.CeilToInt(Mathf.Max(0f, timeLeft)) + "s";

        if (messageResetTimer > 0f)
        {
            messageResetTimer -= Time.deltaTime;
            if (messageResetTimer <= 0f && messageText)
            {
                messageText.text = hasKey
                    ? "EXIT GATE UNLOCKED (GREEN)! Follow the overhead arrow to escape!"
                    : defaultControlsHint;
            }
        }

        if (timeLeft <= 0f) Finish(false, "TIME'S UP!");
    }

    public void SetTotalCoins(int n)
    {
        totalCoins = n;
        Refresh();
    }

    public void AddCoin()
    {
        if (ended) return;
        coinsCollected++;
        score += 10;
        totalScoreAcrossLevels += 10;
        Refresh();
    }

    public void CollectKey()
    {
        if (ended) return;
        hasKey = true;
        score += 50;
        totalScoreAcrossLevels += 50;
        if (exitGate) exitGate.UnlockGate();
        Refresh();
        ShowTemporaryMessage("GOLDEN KEY FOUND! The Exit Gate is now OPEN (Green) - Follow the arrow out!");
    }

    public void ShowTemporaryMessage(string msg)
    {
        if (messageText) messageText.text = msg;
        messageResetTimer = 4.0f;
    }

    public Transform GetObjectiveTarget()
    {
        if (!hasKey && keyTransform) return keyTransform;
        if (exitGate) return exitGate.transform;
        return null;
    }

    void Refresh()
    {
        string keyLabel = hasKey ? "KEY: UNLOCKED" : "KEY: LOCKED";
        if (scoreText)
        {
            scoreText.text = "LEVEL " + currentLevel + " / " + maxLevel + "   |   " + keyLabel + "   |   COINS: " + coinsCollected + " / " + totalCoins;
        }
    }

    public void Finish(bool won, string title)
    {
        if (ended) return;
        ended = true;
        wonThisLevel = won;

        if (endPanel) endPanel.SetActive(true);
        if (endTitle) endTitle.text = title;

        if (endDetails)
        {
            if (won)
            {
                if (currentLevel < maxLevel)
                    endDetails.text = "Great job! Level " + currentLevel + " cleared!  |  Score: " + totalScoreAcrossLevels;
                else
                    endDetails.text = "CHAMPION! You completed all " + maxLevel + " Levels!  |  Final Score: " + totalScoreAcrossLevels;
            }
            else
            {
                endDetails.text = "You ran out of time on Level " + currentLevel + ". Try again!";
            }
        }

        if (primaryActionText)
        {
            if (won)
                primaryActionText.text = (currentLevel < maxLevel) ? "NEXT LEVEL (LVL " + (currentLevel + 1) + ")" : "PLAY AGAIN (LVL 1)";
            else
                primaryActionText.text = "RETRY LEVEL " + currentLevel;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        NeonPlayer p = UnityEngine.Object.FindFirstObjectByType<NeonPlayer>();
        if (p) p.enabled = false;
    }

    // Guaranteed clickable OnGUI modal button in case Unity's Input System blocks UGUI EventSystem
    void OnGUI()
    {
        if (!ended) return;

        float boxW = 420f;
        float boxH = 70f;
        float boxX = (Screen.width - boxW) * 0.5f;
        float boxY = (Screen.height * 0.5f) + 65f;

        GUI.backgroundColor = new Color(0.18f, 0.80f, 0.38f, 1f);
        GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
        btnStyle.fontSize = 18;
        btnStyle.fontStyle = FontStyle.Bold;
        btnStyle.normal.textColor = Color.white;

        string btnLabel;
        if (wonThisLevel)
            btnLabel = (currentLevel < maxLevel) ? "CLICK HERE FOR NEXT LEVEL (or Press Enter)" : "CLICK HERE TO PLAY AGAIN (or Press Enter)";
        else
            btnLabel = "CLICK HERE TO RETRY LEVEL (or Press R)";

        if (GUI.Button(new Rect(boxX, boxY, boxW, boxH), btnLabel, btnStyle))
        {
            OnPrimaryButtonClicked();
        }
    }

    public void OnPrimaryButtonClicked()
    {
        if (wonThisLevel)
        {
            if (currentLevel < maxLevel)
            {
                currentLevel++;
            }
            else
            {
                currentLevel = 1;
                totalScoreAcrossLevels = 0;
            }
        }
        LoadOrRebuildLevel();
    }

    public void RestartCurrentLevel()
    {
        LoadOrRebuildLevel();
    }

    void LoadOrRebuildLevel()
    {
        string[] cleanupNames = new string[] { "COLOURFUL MAZE WORLD", "Runner", "HUD", "Game Manager" };
        foreach (string n in cleanupNames)
        {
            GameObject go = GameObject.Find(n);
            if (go != null)
            {
                go.name = n + "_Old";
                go.SetActive(false);
                SafeDestroy(go);
            }
        }
        BuildCurrentLevel();
    }

    public static void SafeDestroy(UnityEngine.Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying)
            Destroy(obj);
        else
            DestroyImmediate(obj);
    }

    public static void BuildCurrentLevel()
    {
        if (currentLevel < 1) currentLevel = 1;
        // Level 1 = 17x17, Level 2 = 21x21, Level 3 = 25x25, Level 4 = 29x29, Level 5 = 33x33
        int size = 13 + (currentLevel * 4);
        BuildWorldRuntime(size, size);
    }

    public static void BuildWorldRuntime(int W, int H)
    {
        GameObject root = new GameObject("COLOURFUL MAZE WORLD");
        int[,] grid = GenerateMazeData(W, H);

        Material floorMat = CreateRuntimeMat(new Color(0.95f, 0.93f, 0.86f), 0.15f);
        Material tileAccentMat = CreateRuntimeMat(new Color(0.88f, 0.85f, 0.76f), 0.15f);
        Material trimMat = CreateRuntimeMat(new Color(0.98f, 0.98f, 0.95f), 0.25f);
        Material borderMat = CreateRuntimeMat(new Color(0.26f, 0.56f, 0.68f), 0.2f);
        Material coinMat = CreateRuntimeMat(new Color(1.0f, 0.78f, 0.12f), 0.55f);
        Material keyGoldMat = CreateRuntimeMat(new Color(1.0f, 0.84f, 0.08f), 0.65f);
        Material speedPadMat = CreateRuntimeMat(new Color(0.18f, 0.88f, 0.84f), 0.4f);
        Material lockedRedMat = CreateRuntimeMat(new Color(0.92f, 0.25f, 0.25f), 0.35f);

        Material[] wallPalette = new Material[]
        {
            CreateRuntimeMat(new Color(0.95f, 0.42f, 0.38f), 0.2f),
            CreateRuntimeMat(new Color(0.98f, 0.76f, 0.24f), 0.2f),
            CreateRuntimeMat(new Color(0.32f, 0.82f, 0.58f), 0.2f),
            CreateRuntimeMat(new Color(0.34f, 0.68f, 0.96f), 0.2f),
            CreateRuntimeMat(new Color(0.68f, 0.50f, 0.92f), 0.2f),
            CreateRuntimeMat(new Color(0.98f, 0.56f, 0.26f), 0.2f)
        };

        SpawnCube("Arena Floor", new Vector3(W / 2f - 0.5f, -0.25f, H / 2f - 0.5f), new Vector3(W + 4f, 0.5f, H + 4f), floorMat, root.transform);

        for (int z = 0; z < H; z++)
        {
            for (int x = 0; x < W; x++)
            {
                if (grid[z, x] == 1)
                {
                    Material chosenWallMat = wallPalette[(x * 3 + z * 5 + currentLevel) % wallPalette.Length];
                    SpawnCube("Colourful Wall", new Vector3(x, 1.0f, z), new Vector3(0.96f, 2.0f, 0.96f), chosenWallMat, root.transform);
                    GameObject cap = SpawnCube("Wall Top Trim", new Vector3(x, 2.03f, z), new Vector3(0.98f, 0.06f, 0.98f), trimMat, root.transform);
                    SafeDestroy(cap.GetComponent<Collider>());
                }
                else if ((x + z) % 2 == 1)
                {
                    GameObject tile = SpawnCube("Floor Tile", new Vector3(x, 0.005f, z), new Vector3(0.92f, 0.01f, 0.92f), tileAccentMat, root.transform);
                    SafeDestroy(tile.GetComponent<Collider>());
                }
            }
        }

        for (int x = -1; x <= W; x++)
        {
            SpawnCube("Boundary", new Vector3(x, 1.05f, -1), new Vector3(1.05f, 2.1f, 1.05f), borderMat, root.transform);
            SpawnCube("Boundary", new Vector3(x, 1.05f, H), new Vector3(1.05f, 2.1f, 1.05f), borderMat, root.transform);
        }
        for (int z = 0; z < H; z++)
        {
            SpawnCube("Boundary", new Vector3(-1, 1.05f, z), new Vector3(1.05f, 2.1f, 1.05f), borderMat, root.transform);
            SpawnCube("Boundary", new Vector3(W, 1.05f, z), new Vector3(1.05f, 2.1f, 1.05f), borderMat, root.transform);
        }

        GameObject player = SpawnRunnerCharacter();

        Camera cam = Camera.main;
        if (!cam)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.55f, 0.82f, 0.96f);
        cam.fieldOfView = 52f;

        NeonCameraFollow camFollow = cam.GetComponent<NeonCameraFollow>();
        if (!camFollow) camFollow = cam.gameObject.AddComponent<NeonCameraFollow>();
        camFollow.target = player.transform;
        camFollow.ApplyPreset(0);
        camFollow.SnapToTarget();

        player.GetComponent<NeonPlayer>().cameraPivot = cam.transform;

        Vector2Int keyCell = new Vector2Int(1, H - 2);
        grid[keyCell.y, keyCell.x] = 0;
        GameObject goldenKeyObj = BuildGoldenKey(new Vector3(keyCell.x, 0.65f, keyCell.y), keyGoldMat, trimMat, root.transform);

        int coinCount = 0;
        int coinMod = Mathf.Max(8, 15 - currentLevel);
        int padMod = Mathf.Max(15, 32 - currentLevel * 2);

        for (int z = 1; z < H - 1; z++)
        {
            for (int x = 1; x < W - 1; x++)
            {
                if (grid[z, x] == 0 && !(x == 1 && z == 1) && !(x == keyCell.x && z == keyCell.y) && !(x >= W - 3 && z >= H - 3))
                {
                    if ((x * 7 + z * 13) % coinMod == 0)
                    {
                        GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        c.name = "Bonus Coin";
                        c.transform.SetParent(root.transform);
                        c.transform.position = new Vector3(x, 0.55f, z);
                        c.transform.localScale = new Vector3(0.45f, 0.10f, 0.45f);
                        c.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                        c.GetComponent<Renderer>().sharedMaterial = coinMat;
                        c.GetComponent<Collider>().isTrigger = true;
                        NeonCoin coinScript = c.AddComponent<NeonCoin>();
                        coinScript.itemKind = NeonCoin.ItemKind.Coin;
                        coinCount++;
                    }
                    else if ((x * 11 + z * 5) % padMod == 0)
                    {
                        GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        pad.name = "Speed Boost Pad";
                        pad.transform.SetParent(root.transform);
                        pad.transform.position = new Vector3(x, 0.03f, z);
                        pad.transform.localScale = new Vector3(0.75f, 0.03f, 0.75f);
                        pad.GetComponent<Renderer>().sharedMaterial = speedPadMat;
                        pad.GetComponent<Collider>().isTrigger = true;
                        NeonCoin padScript = pad.AddComponent<NeonCoin>();
                        padScript.itemKind = NeonCoin.ItemKind.SpeedBoostPad;
                    }
                }
            }
        }

        NeonExit exitGate = BuildLockedExitGate(new Vector3(W - 2, 0f, H - 2), trimMat, lockedRedMat, root.transform);

        RenderSettings.ambientLight = new Color(0.75f, 0.78f, 0.84f);
        RenderSettings.fog = false;
        Light light = UnityEngine.Object.FindFirstObjectByType<Light>();
        if (!light)
        {
            GameObject lightObj = new GameObject("Daylight Sun");
            light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
        }
        light.intensity = 1.15f;
        light.color = new Color(1.0f, 0.97f, 0.90f);
        light.shadows = LightShadows.Soft;
        light.transform.rotation = Quaternion.Euler(52f, -35f, 0f);

        SpawnUI(coinCount, goldenKeyObj.transform, exitGate);
    }

    static GameObject SpawnRunnerCharacter()
    {
        GameObject player = new GameObject("Runner");
        player.transform.position = new Vector3(1f, 0.1f, 1f);
        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 1.4f;
        cc.radius = 0.32f;
        cc.center = new Vector3(0f, 0.7f, 0f);

        NeonPlayer np = player.AddComponent<NeonPlayer>();
        np.useDogCharacter = true;

        Transform visualRoot = new GameObject("CharacterVisualRoot").transform;
        visualRoot.SetParent(player.transform, false);
        np.visualRoot = visualRoot;

        Material dogFur = CreateRuntimeMat(new Color(0.92f, 0.64f, 0.26f), 0.25f);
        Material dogCream = CreateRuntimeMat(new Color(0.98f, 0.90f, 0.74f), 0.25f);
        Material darkBrown = CreateRuntimeMat(new Color(0.25f, 0.16f, 0.10f), 0.2f);
        Material collarRed = CreateRuntimeMat(new Color(0.92f, 0.22f, 0.22f), 0.3f);

        Transform dogGroup = new GameObject("DogModel").transform;
        dogGroup.SetParent(visualRoot, false);
        np.dogGroup = dogGroup;

        SpawnSubPart(PrimitiveType.Cube, "Body", dogGroup, new Vector3(0f, 0.38f, 0f), new Vector3(0.42f, 0.34f, 0.68f), dogFur);
        SpawnSubPart(PrimitiveType.Cube, "Chest", dogGroup, new Vector3(0f, 0.40f, 0.22f), new Vector3(0.38f, 0.32f, 0.30f), dogCream);
        SpawnSubPart(PrimitiveType.Cube, "Collar", dogGroup, new Vector3(0f, 0.50f, 0.26f), new Vector3(0.40f, 0.10f, 0.18f), collarRed);
        SpawnSubPart(PrimitiveType.Cube, "Head", dogGroup, new Vector3(0f, 0.66f, 0.32f), new Vector3(0.38f, 0.34f, 0.36f), dogFur);
        SpawnSubPart(PrimitiveType.Cube, "Snout", dogGroup, new Vector3(0f, 0.60f, 0.54f), new Vector3(0.22f, 0.18f, 0.20f), dogCream);
        SpawnSubPart(PrimitiveType.Cube, "Nose", dogGroup, new Vector3(0f, 0.66f, 0.64f), new Vector3(0.10f, 0.08f, 0.06f), darkBrown);
        SpawnSubPart(PrimitiveType.Sphere, "EyeL", dogGroup, new Vector3(-0.10f, 0.72f, 0.50f), new Vector3(0.07f, 0.07f, 0.07f), darkBrown);
        SpawnSubPart(PrimitiveType.Sphere, "EyeR", dogGroup, new Vector3(0.10f, 0.72f, 0.50f), new Vector3(0.07f, 0.07f, 0.07f), darkBrown);
        SpawnSubPart(PrimitiveType.Cube, "EarL", dogGroup, new Vector3(-0.22f, 0.64f, 0.30f), new Vector3(0.08f, 0.26f, 0.14f), darkBrown);
        SpawnSubPart(PrimitiveType.Cube, "EarR", dogGroup, new Vector3(0.22f, 0.64f, 0.30f), new Vector3(0.08f, 0.26f, 0.14f), darkBrown);

        Transform tailPivot = new GameObject("TailPivot").transform;
        tailPivot.SetParent(dogGroup, false);
        tailPivot.localPosition = new Vector3(0f, 0.50f, -0.32f);
        SpawnSubPart(PrimitiveType.Cube, "Tail", tailPivot, new Vector3(0f, 0.14f, -0.04f), new Vector3(0.10f, 0.28f, 0.10f), dogFur);
        np.dogTail = tailPivot;

        np.dogLegs = new Transform[4];
        np.dogLegs[0] = SpawnLimb("LegFL", dogGroup, new Vector3(-0.14f, 0.24f, 0.22f), new Vector3(0.12f, 0.24f, 0.12f), dogCream);
        np.dogLegs[1] = SpawnLimb("LegFR", dogGroup, new Vector3(0.14f, 0.24f, 0.22f), new Vector3(0.12f, 0.24f, 0.12f), dogCream);
        np.dogLegs[2] = SpawnLimb("LegBL", dogGroup, new Vector3(-0.14f, 0.24f, -0.22f), new Vector3(0.12f, 0.24f, 0.12f), dogFur);
        np.dogLegs[3] = SpawnLimb("LegBR", dogGroup, new Vector3(0.14f, 0.24f, -0.22f), new Vector3(0.12f, 0.24f, 0.12f), dogFur);

        Material shirtMat = CreateRuntimeMat(new Color(0.20f, 0.56f, 0.94f), 0.25f);
        Material pantsMat = CreateRuntimeMat(new Color(0.22f, 0.28f, 0.38f), 0.2f);
        Material skinMat = CreateRuntimeMat(new Color(0.96f, 0.78f, 0.64f), 0.2f);
        Material capMat = CreateRuntimeMat(new Color(0.98f, 0.76f, 0.18f), 0.3f);

        Transform humanGroup = new GameObject("HumanModel").transform;
        humanGroup.SetParent(visualRoot, false);
        np.humanGroup = humanGroup;

        SpawnSubPart(PrimitiveType.Cube, "Torso", humanGroup, new Vector3(0f, 0.64f, 0f), new Vector3(0.40f, 0.46f, 0.24f), shirtMat);
        SpawnSubPart(PrimitiveType.Cube, "Backpack", humanGroup, new Vector3(0f, 0.66f, -0.17f), new Vector3(0.32f, 0.34f, 0.14f), collarRed);
        SpawnSubPart(PrimitiveType.Cube, "Head", humanGroup, new Vector3(0f, 1.05f, 0f), new Vector3(0.32f, 0.32f, 0.32f), skinMat);
        SpawnSubPart(PrimitiveType.Cube, "Cap", humanGroup, new Vector3(0f, 1.22f, 0.04f), new Vector3(0.34f, 0.10f, 0.40f), capMat);

        np.humanLimbs = new Transform[4];
        np.humanLimbs[0] = SpawnLimb("HumanLegL", humanGroup, new Vector3(-0.11f, 0.40f, 0f), new Vector3(0.14f, 0.40f, 0.14f), pantsMat);
        np.humanLimbs[1] = SpawnLimb("HumanLegR", humanGroup, new Vector3(0.11f, 0.40f, 0f), new Vector3(0.14f, 0.40f, 0.14f), pantsMat);
        np.humanLimbs[2] = SpawnLimb("HumanArmL", humanGroup, new Vector3(-0.28f, 0.84f, 0f), new Vector3(0.12f, 0.36f, 0.12f), skinMat);
        np.humanLimbs[3] = SpawnLimb("HumanArmR", humanGroup, new Vector3(0.28f, 0.84f, 0f), new Vector3(0.12f, 0.36f, 0.12f), skinMat);
        humanGroup.gameObject.SetActive(false);

        Transform compass = new GameObject("CompassArrow").transform;
        compass.SetParent(player.transform, false);
        compass.localPosition = new Vector3(0f, 1.55f, 0f);
        SpawnSubPart(PrimitiveType.Cube, "ArrowShaft", compass, new Vector3(0f, 0f, 0.18f), new Vector3(0.10f, 0.05f, 0.32f), capMat);
        GameObject tipL = SpawnSubPart(PrimitiveType.Cube, "ArrowTipL", compass, new Vector3(-0.07f, 0f, 0.30f), new Vector3(0.16f, 0.05f, 0.07f), capMat);
        tipL.transform.localRotation = Quaternion.Euler(0f, 35f, 0f);
        GameObject tipR = SpawnSubPart(PrimitiveType.Cube, "ArrowTipR", compass, new Vector3(0.07f, 0f, 0.30f), new Vector3(0.16f, 0.05f, 0.07f), capMat);
        tipR.transform.localRotation = Quaternion.Euler(0f, -35f, 0f);
        np.compassArrow = compass;

        return player;
    }

    static GameObject BuildGoldenKey(Vector3 pos, Material goldMat, Material baseMat, Transform parent)
    {
        GameObject keyObj = new GameObject("GOLDEN KEY");
        keyObj.transform.SetParent(parent);
        keyObj.transform.position = pos;

        SphereCollider trig = keyObj.AddComponent<SphereCollider>();
        trig.isTrigger = true;
        trig.radius = 0.65f;

        NeonCoin coinScript = keyObj.AddComponent<NeonCoin>();
        coinScript.itemKind = NeonCoin.ItemKind.GoldenKey;
        coinScript.spinSpeed = 95f;

        GameObject ring = SpawnSubPart(PrimitiveType.Cylinder, "KeyRing", keyObj.transform, new Vector3(0f, 0.28f, 0f), new Vector3(0.42f, 0.07f, 0.42f), goldMat);
        ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        SpawnSubPart(PrimitiveType.Cube, "KeyShaft", keyObj.transform, new Vector3(0f, -0.10f, 0f), new Vector3(0.12f, 0.56f, 0.12f), goldMat);
        SpawnSubPart(PrimitiveType.Cube, "KeyTooth1", keyObj.transform, new Vector3(0.14f, -0.20f, 0f), new Vector3(0.18f, 0.09f, 0.10f), goldMat);
        SpawnSubPart(PrimitiveType.Cube, "KeyTooth2", keyObj.transform, new Vector3(0.12f, -0.34f, 0f), new Vector3(0.14f, 0.09f, 0.10f), goldMat);

        GameObject ped = SpawnSubPart(PrimitiveType.Cylinder, "KeyPedestal", parent, new Vector3(pos.x, 0.03f, pos.z), new Vector3(0.9f, 0.03f, 0.9f), baseMat);
        SafeDestroy(ped.GetComponent<Collider>());

        return keyObj;
    }

    static NeonExit BuildLockedExitGate(Vector3 pos, Material pillarMat, Material gateMat, Transform parent)
    {
        GameObject exitRoot = new GameObject("EXIT PORTAL");
        exitRoot.transform.SetParent(parent);
        exitRoot.transform.position = pos;

        BoxCollider trigger = exitRoot.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 1.1f, 0f);
        trigger.size = new Vector3(1.1f, 2.2f, 1.1f);

        NeonExit exitScript = exitRoot.AddComponent<NeonExit>();

        SpawnSubPart(PrimitiveType.Cube, "LeftPillar", exitRoot.transform, new Vector3(-0.42f, 1.15f, 0f), new Vector3(0.18f, 2.3f, 0.28f), pillarMat);
        SpawnSubPart(PrimitiveType.Cube, "RightPillar", exitRoot.transform, new Vector3(0.42f, 1.15f, 0f), new Vector3(0.18f, 2.3f, 0.28f), pillarMat);
        GameObject topBar = SpawnSubPart(PrimitiveType.Cube, "TopBanner", exitRoot.transform, new Vector3(0f, 2.35f, 0f), new Vector3(1.08f, 0.28f, 0.34f), gateMat);

        GameObject barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
        barrier.name = "LockedBarrierDoor";
        barrier.transform.SetParent(exitRoot.transform, false);
        barrier.transform.localPosition = new Vector3(0f, 1.05f, 0f);
        barrier.transform.localScale = new Vector3(0.72f, 2.1f, 0.18f);
        barrier.GetComponent<Renderer>().sharedMaterial = gateMat;

        exitScript.barrierDoor = barrier.transform;
        exitScript.gateRenderers = new Renderer[]
        {
            topBar.GetComponent<Renderer>(),
            barrier.GetComponent<Renderer>()
        };

        return exitScript;
    }

    static Transform SpawnLimb(string name, Transform parent, Vector3 pivotPos, Vector3 scale, Material mat)
    {
        Transform pivot = new GameObject(name).transform;
        pivot.SetParent(parent, false);
        pivot.localPosition = pivotPos;
        SpawnSubPart(PrimitiveType.Cube, "Mesh", pivot, new Vector3(0f, -scale.y * 0.5f, 0f), scale, mat);
        return pivot;
    }

    static GameObject SpawnSubPart(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
    {
        GameObject o = GameObject.CreatePrimitive(type);
        o.name = name;
        o.transform.SetParent(parent, false);
        o.transform.localPosition = localPos;
        o.transform.localScale = localScale;
        Collider col = o.GetComponent<Collider>();
        if (col) SafeDestroy(col);
        o.GetComponent<Renderer>().sharedMaterial = mat;
        return o;
    }

    static GameObject SpawnCube(string n, Vector3 p, Vector3 s, Material m, Transform parent)
    {
        GameObject o = GameObject.CreatePrimitive(PrimitiveType.Cube);
        o.name = n;
        o.transform.position = p;
        o.transform.localScale = s;
        o.transform.SetParent(parent);
        o.GetComponent<Renderer>().sharedMaterial = m;
        return o;
    }

    static Material CreateRuntimeMat(Color c, float smoothness)
    {
        Shader sh = null;
        if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null)
            sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        if (sh == null) sh = Shader.Find("Legacy Shaders/Diffuse");

        Material m = new Material(sh);
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
        return m;
    }

    public static int[,] GenerateMazeData(int w, int h)
    {
        int[,] a = new int[h, w];
        for (int z = 0; z < h; z++)
            for (int x = 0; x < w; x++)
                a[z, x] = 1;

        System.Random r = new System.Random(42 + currentLevel * 7);
        Stack<Vector2Int> st = new Stack<Vector2Int>();
        st.Push(new Vector2Int(1, 1));
        a[1, 1] = 0;
        int[] dx = { 0, 2, 0, -2 }, dz = { 2, 0, -2, 0 };
        while (st.Count > 0)
        {
            Vector2Int p = st.Peek();
            List<int> dirs = new List<int>();
            for (int d = 0; d < 4; d++)
            {
                int nx = p.x + dx[d], nz = p.y + dz[d];
                if (nx > 0 && nz > 0 && nx < w - 1 && nz < h - 1 && a[nz, nx] == 1)
                    dirs.Add(d);
            }
            if (dirs.Count == 0)
            {
                st.Pop();
                continue;
            }
            int k = dirs[r.Next(dirs.Count)], nx2 = p.x + dx[k], nz2 = p.y + dz[k];
            a[p.y + dz[k] / 2, p.x + dx[k] / 2] = 0;
            a[nz2, nx2] = 0;
            st.Push(new Vector2Int(nx2, nz2));
        }
        for (int i = 0; i < 30 + currentLevel * 6; i++)
        {
            int x = r.Next(1, w - 1), z = r.Next(1, h - 1);
            if (a[z, x] == 1 && ((a[z, x - 1] == 0 && a[z, x + 1] == 0) || (a[z - 1, x] == 0 && a[z + 1, x] == 0)))
                a[z, x] = 0;
        }
        a[h - 2, w - 2] = 0;
        a[h - 2, w - 3] = 0;
        a[h - 3, w - 2] = 0;
        a[h - 2, 1] = 0;
        a[h - 3, 1] = 0;
        return a;
    }

    static void SpawnUI(int count, Transform keyTransform, NeonExit exitGate)
    {
        GameObject game = new GameObject("Game Manager");
        NeonMazeGame gm = game.AddComponent<NeonMazeGame>();
        gm.keyTransform = keyTransform;
        gm.exitGate = exitGate;
        gm.SetTotalCoins(count);

        GameObject canvasObj = new GameObject("HUD");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();
        raycaster.ignoreReversedGraphics = true;
        raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;

        if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Top-Left Card (Separate parent Image and child Text so Unity UI never conflicts!)
        gm.scoreText = CreateCardWithText(
            canvas.transform,
            "TopLeftCard",
            "LEVEL " + currentLevel + " / 5   |   KEY: LOCKED   |   COINS: 0 / " + count,
            new Vector2(18, -16),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(450, 44),
            new Color(0.10f, 0.15f, 0.24f, 0.88f),
            18,
            TextAnchor.MiddleCenter
        );

        // Top-Right Timer Card
        gm.timerText = CreateCardWithText(
            canvas.transform,
            "TopRightTimerCard",
            "TIME: 150s",
            new Vector2(-18, -16),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(170, 44),
            new Color(0.10f, 0.15f, 0.24f, 0.88f),
            20,
            TextAnchor.MiddleCenter
        );

        // Bottom Controls Bar
        gm.messageText = CreateCardWithText(
            canvas.transform,
            "BottomHintCard",
            "WASD: Move  |  [C] Dog/Human  |  [V] Camera Angle  |  [Q/E] Rotate  |  [R] Restart",
            new Vector2(0, 16),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(760, 38),
            new Color(0.10f, 0.15f, 0.24f, 0.88f),
            15,
            TextAnchor.MiddleCenter
        );

        // Center Victory / Try Again Modal Panel
        GameObject panel = new GameObject("End Panel");
        panel.transform.SetParent(canvas.transform, false);
        Image img = panel.AddComponent<Image>();
        img.color = new Color(0.06f, 0.10f, 0.18f, 0.95f);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(480, 250);
        rt.anchoredPosition = Vector2.zero;

        gm.endPanel = panel;
        gm.endTitle = CreatePureText(panel.transform, "EndTitle", "LEVEL COMPLETED!", new Vector2(0, -28), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(440, 44), 28, TextAnchor.MiddleCenter);
        gm.endDetails = CreatePureText(panel.transform, "EndDetails", "Great job!", new Vector2(0, 18), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(440, 60), 18, TextAnchor.MiddleCenter);

        GameObject btnObj = new GameObject("PrimaryActionButton");
        btnObj.transform.SetParent(panel.transform, false);
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.18f, 0.78f, 0.38f, 1f);
        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImg;

        RectTransform btnRt = btnObj.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0f);
        btnRt.anchorMax = new Vector2(0.5f, 0f);
        btnRt.pivot = new Vector2(0.5f, 0f);
        btnRt.anchoredPosition = new Vector2(0, 22);
        btnRt.sizeDelta = new Vector2(280, 48);

        gm.primaryActionButton = btn;
        gm.primaryActionText = CreatePureText(btnObj.transform, "ButtonLabel", "NEXT LEVEL", Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(280, 48), 18, TextAnchor.MiddleCenter);
        btn.onClick.AddListener(gm.OnPrimaryButtonClicked);

        panel.SetActive(false);
    }

    public static Font GetBuiltinFontSafe()
    {
        Font f = null;
        try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
        if (f == null)
        {
            try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
        }
        if (f == null)
        {
            f = Font.CreateDynamicFontFromOSFont("Arial", 16);
        }
        return f;
    }

    // Properly separates the background Image GameObject and child Text GameObject
    static Text CreateCardWithText(Transform parent, string cardName, string value, Vector2 pos, Vector2 anchor, Vector2 pivot, Vector2 sizeDelta, Color bgColor, int fontSize, TextAnchor align)
    {
        GameObject cardObj = new GameObject(cardName);
        cardObj.transform.SetParent(parent, false);
        Image bg = cardObj.AddComponent<Image>();
        bg.color = bgColor;

        RectTransform cardRt = cardObj.GetComponent<RectTransform>();
        cardRt.anchorMin = anchor;
        cardRt.anchorMax = anchor;
        cardRt.pivot = pivot;
        cardRt.anchoredPosition = pos;
        cardRt.sizeDelta = sizeDelta;

        return CreatePureText(cardObj.transform, "Label", value, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), sizeDelta - new Vector2(16f, 4f), fontSize, align);
    }

    static Text CreatePureText(Transform parent, string objName, string value, Vector2 pos, Vector2 anchor, Vector2 pivot, Vector2 sizeDelta, int fontSize, TextAnchor align)
    {
        GameObject textObj = new GameObject(objName);
        textObj.transform.SetParent(parent, false);

        Text t = textObj.AddComponent<Text>();
        t.text = value;
        t.font = GetBuiltinFontSafe();
        t.fontSize = fontSize;
        t.fontStyle = FontStyle.Bold;
        t.color = Color.white;
        t.alignment = align;

        RectTransform r = t.rectTransform;
        r.anchorMin = anchor;
        r.anchorMax = anchor;
        r.pivot = pivot;
        r.anchoredPosition = pos;
        r.sizeDelta = sizeDelta;
        return t;
    }
}
