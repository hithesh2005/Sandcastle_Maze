// MaizeMazeGame.UI.cs  -  Sandcastle Shores animated interface.
//   Main menu (3D maze flyover behind it) / difficulty cards / settings /
//   HUD / toasts / pause / end screens with stars.
// All art (rounded panels, hearts, stars, arrow, vignette) is generated in
// code by MazeVisuals.cs - no images needed.
using System.Collections.Generic;
using UnityEngine;

public partial class MaizeMazeGame
{
    // ------------------------------------------------------------ palette
    static readonly Color cCard = new Color(.035f, .105f, .145f, .94f);
    static readonly Color cGold = new Color(1f, .77f, .43f);
    static readonly Color cTeal = new Color(.34f, .91f, .88f);
    static readonly Color cRed = new Color(.98f, .42f, .34f);
    static readonly Color cMuted = new Color(.94f, .88f, .75f);

    enum Page { Main, Select, Settings }
    Page page = Page.Main;
    float pageStart;
    int pausePage;     // 0 = pause menu, 1 = settings
    int keyboardMenuChoice;
    int keyboardDifficultyChoice;

    void SetPage(Page p)
    {
        page = p;
        pageStart = uiClock;
        GUI.FocusControl(null);
        if (p == Page.Main) keyboardMenuChoice = 0;
        if (p == Page.Select) keyboardDifficultyChoice = 0;
    }
    float PageT { get { return uiClock - pageStart; } }

    // ------------------------------------------------------ ui resources
    Texture2D tRound, tRoundM, tRoundS, tCircle, tGlow, tHeart, tStar, tArrow, tVig;
    GUIStyle sPanel, sPanelM, sPanelS;
    Font uiFont;
    bool uiInit;
    float UW, UH, uiS;
    readonly Dictionary<string, GUIStyle> styles = new Dictionary<string, GUIStyle>();
    readonly Dictionary<string, float> hov = new Dictionary<string, float>();
    string lastHover = "";

    void InitUI()
    {
        if (uiInit) return;
        uiInit = true;
        tRound = MazeVisuals.RoundedRect(64, 20);
        tRoundM = MazeVisuals.RoundedRect(32, 10);
        tRoundS = MazeVisuals.RoundedRect(16, 5);
        tCircle = MazeVisuals.Circle(64);
        tGlow = MazeVisuals.Glow(128);
        tHeart = MazeVisuals.Heart(48);
        tStar = MazeVisuals.Star(64);
        tArrow = MazeVisuals.Arrow(64);
        tVig = MazeVisuals.Vignette(128);
        sPanel = NineSlice(tRound, 20);
        sPanelM = NineSlice(tRoundM, 10);
        sPanelS = NineSlice(tRoundS, 5);
        try { uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Trebuchet MS", "Arial Rounded MT Bold", "Segoe UI", "Arial" }, 28); }
        catch { uiFont = null; }
    }

    GUIStyle NineSlice(Texture2D t, int b)
    {
        var s = new GUIStyle();
        s.normal.background = t;
        s.border = new RectOffset(b, b, b, b);
        return s;
    }

    GUIStyle S(int size, TextAnchor al = TextAnchor.MiddleLeft, bool bold = true, bool wrap = false)
    {
        string k = size + "|" + al + "|" + bold + "|" + wrap;
        GUIStyle st;
        if (styles.TryGetValue(k, out st)) return st;
        st = new GUIStyle(GUI.skin.label);
        st.fontSize = size; st.alignment = al; st.wordWrap = wrap;
        st.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        if (uiFont != null) st.font = uiFont;
        styles[k] = st;
        return st;
    }

    // ------------------------------------------------------- draw helpers
    // IMGUI can call OnGUI for Layout, MouseDown, MouseUp and Repaint.  A
    // GUIStyle.Draw call is only legal during Repaint.  Keep all painting in
    // these helpers so input events can still reach the hit-tests below.
    bool IsRepaintEvent
    {
        get { return Event.current == null || Event.current.type == EventType.Repaint; }
    }

    void Panel(Rect r, Color c)
    {
        if (!IsRepaintEvent) return;
        Color old = GUI.color;
        try
        {
            GUI.color = c;
            sPanel.Draw(r, GUIContent.none, false, false, false, false);
        }
        finally { GUI.color = old; }
        // Fine warm highlight gives every major card a consistent edge.
        Fill(new Rect(r.x + 14, r.y + 1, Mathf.Max(0, r.width - 28), 2),
             new Color(cGold.r, cGold.g, cGold.b, Mathf.Clamp01(c.a) * 0.34f));
    }

    void PanelM(Rect r, Color c)
    {
        if (!IsRepaintEvent) return;
        Color old = GUI.color;
        try { GUI.color = c; sPanelM.Draw(r, GUIContent.none, false, false, false, false); }
        finally { GUI.color = old; }
    }

    void PanelS(Rect r, Color c)
    {
        if (!IsRepaintEvent) return;
        Color old = GUI.color;
        try { GUI.color = c; sPanelS.Draw(r, GUIContent.none, false, false, false, false); }
        finally { GUI.color = old; }
    }

    void Fill(Rect r, Color c)
    {
        if (!IsRepaintEvent) return;
        Color old = GUI.color;
        try { GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); }
        finally { GUI.color = old; }
    }

    void Tex(Rect r, Texture2D t, Color c)
    {
        if (!IsRepaintEvent || t == null) return;
        Color old = GUI.color;
        try { GUI.color = c; GUI.DrawTexture(r, t); }
        finally { GUI.color = old; }
    }

    void T(Rect r, string s, GUIStyle st, Color c, bool shadow = true)
    {
        if (!IsRepaintEvent) return;
        if (shadow)
        {
            st.normal.textColor = new Color(0, 0, 0, c.a * 0.55f);
            GUI.Label(new Rect(r.x + 1.5f, r.y + 2f, r.width, r.height), s, st);
        }
        st.normal.textColor = c;
        GUI.Label(r, s, st);
    }

    void Bar(Rect r, float f, Color c)
    {
        PanelS(r, new Color(1, 1, 1, .12f));
        if (f > 0.01f) PanelS(new Rect(r.x, r.y, Mathf.Max(r.height, r.width * Mathf.Clamp01(f)), r.height), c);
    }

    float Hover(string id, bool inside)
    {
        float h; hov.TryGetValue(id, out h);
        if (Event.current.type == EventType.Repaint)
        {
            h = Mathf.MoveTowards(h, inside ? 1f : 0f, Time.unscaledDeltaTime * 9f);
            hov[id] = h;
        }
        return h;
    }

    // Mouse input is read through both IMGUI events and Unity's input backend.
    // Cache the raw button edge once per frame: OnGUI may run several times.
    int clickFrame = -1;
    bool clickAvail;
    string activeSlider = "";

    Vector2 MouseGUI()
    {
        Vector2 m = GameInput.MousePos();
        float scale = Mathf.Max(0.001f, uiS);
        return new Vector2(m.x / scale, (Screen.height - m.y) / scale);
    }

    bool ClickPressed()
    {
        if (clickFrame != Time.frameCount)
        {
            clickFrame = Time.frameCount;
            clickAvail = GameInput.MouseDown();
        }
        return clickAvail;
    }

    void ConsumeClick() { clickAvail = false; }

    // Handle a press immediately (on mouse-down), rather than waiting for a
    // mouse-up event. The raw-input fallback covers New Input System projects
    // where the IMGUI button event may not be delivered to OnGUI.
    bool PressedInside(Rect r)
    {
        Event ev = Event.current;
        bool leftMouseDown = ev != null && ev.type == EventType.MouseDown && ev.button == 0;

        // Event.mousePosition can be reported in pre-matrix coordinates in
        // some Editor/Game-view configurations. Check both coordinate spaces.
        if (leftMouseDown)
        {
            Vector2 eventPoint = ev.mousePosition;
            bool eventHit = r.Contains(eventPoint) || r.Contains(eventPoint / Mathf.Max(0.001f, uiS));
            if (eventHit)
            {
                ev.Use();
                ConsumeClick();
                return true;
            }
        }

        // Raw input is a second path for both the legacy and new Input System.
        if (r.Contains(MouseGUI()) && ClickPressed())
        {
            if (leftMouseDown) ev.Use();
            ConsumeClick();
            return true;
        }
        return false;
    }

    bool Button(Rect r, string label, bool primary = false)
    {
        // Let Unity's own IMGUI control process mouse-down/mouse-up events.
        // This avoids depending on Input.mousePosition or Editor Game-view
        // coordinate scaling for menu buttons.
        bool clicked = GUI.Button(r, GUIContent.none, GUIStyle.none);

        bool inside = r.Contains(MouseGUI());
        string id = "b|" + label + "|" + (int)r.x + "|" + (int)r.y;
        float h = Hover(id, inside);
        if (Event.current.type == EventType.Repaint)
        {
            if (inside && lastHover != id) { lastHover = id; Play("hover", 0.5f); }
            else if (!inside && lastHover == id) lastHover = "";
        }
        float g = h * 5f;
        Rect rr = new Rect(r.x - g, r.y - g * 0.5f, r.width + g * 2f, r.height + g);
        Color b0 = primary ? cGold : new Color(1, 1, 1, .08f);
        Color b1 = primary ? new Color(1f, .90f, .50f) : new Color(1, 1, 1, .20f);
        Panel(rr, Color.Lerp(b0, b1, h));
        T(rr, label, S(24, TextAnchor.MiddleCenter), primary ? new Color(.10f, .15f, .18f) : Color.white, false);

        if (clicked)
        {
            Play("click");
            GUI.FocusControl(null);
            return true;
        }
        return false;
    }

    float Slider(Rect r, float v, float min, float max, string id)
    {
        Vector2 mp = MouseGUI();
        if (activeSlider == id)
        {
            if (GameInput.MouseHeld())
                v = Mathf.Lerp(min, max, Mathf.Clamp01((mp.x - r.x) / Mathf.Max(1f, r.width)));
            else activeSlider = "";
        }
        else if (PressedInside(r))
        {
            activeSlider = id;
            v = Mathf.Lerp(min, max, Mathf.Clamp01((mp.x - r.x) / Mathf.Max(1f, r.width)));
        }

        Rect track = new Rect(r.x, r.center.y - 5, r.width, 10);
        float t = Mathf.InverseLerp(min, max, v);
        PanelS(track, new Color(1, 1, 1, .14f));
        PanelS(new Rect(track.x, track.y, Mathf.Max(10, track.width * t), 10), cGold);
        Tex(new Rect(track.x + track.width * t - 13, r.center.y - 13, 26, 26), tCircle, Color.white);
        return v;
    }

    bool Toggle(Rect r, string label, bool v)
    {
        bool next = v;
        if (PressedInside(r))
        {
            next = !v;
            Play("click");
        }
        bool inside = r.Contains(MouseGUI());
        float h = Hover("t|" + label, inside);
        T(new Rect(r.x, r.y, r.width - 90, r.height), label, S(22), Color.Lerp(Color.white, cGold, h));
        Rect sw = new Rect(r.xMax - 70, r.center.y - 15, 64, 30);
        PanelM(sw, next ? cTeal : new Color(1, 1, 1, .20f));
        Tex(new Rect(next ? sw.xMax - 27 : sw.x + 3, sw.y + 3, 24, 24), tCircle, Color.white);
        return next;
    }

    float Ease(int i) { float p = Mathf.Clamp01(PageT * 2.6f - i * 0.18f); return 1f - Mathf.Pow(1f - p, 3f); }

    static string Clock(float secs)
    {
        int t = Mathf.Max(0, Mathf.CeilToInt(secs));
        return (t / 60).ToString("00") + ":" + (t % 60).ToString("00");
    }

    Rect MiniRectPx()
    {
        float h = Screen.height * MM_H;
        return new Rect(Screen.width - h - Screen.width * 0.015f, Screen.height - h - Screen.height * 0.03f, h, h);
    }

    // ================================================================ OnGUI
    void OnGUI()
    {
        Matrix4x4 previousMatrix = GUI.matrix;
        try
        {
            InitUI();
            uiS = Mathf.Max(0.1f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(uiS, uiS, 1));
            UW = Screen.width / uiS; UH = 720f;

            switch (state)
            {
                case State.Menu: DrawMenu(); break;
                case State.Playing: DrawFx(); DrawHUD(); DrawToasts(); DrawBanner(); break;
                case State.Paused: DrawFx(); DrawHUD(); DrawPause(); break;
                case State.Won: DrawFx(); DrawEnd(true); break;
                case State.Lost: DrawFx(); DrawEnd(false); break;
            }
        }
        finally
        {
            GUI.matrix = previousMatrix;
        }
    }

    // ================================================================ MENU
    void DrawMenu()
    {
        Tex(new Rect(0, 0, UW, UH), tVig, new Color(0, 0, 0, .75f));
        Fill(new Rect(0, 0, UW, UH), new Color(0, 0, 0, .18f));
        for (int i = 0; i < 9; i++)
        {
            float x = (i * 211f + uiClock * (10 + i * 3)) % (UW + 260) - 130;
            float y = (i * 97f) % UH + Mathf.Sin(uiClock * 0.7f + i) * 30f;
            float sz = 70 + (i % 4) * 34;
            Tex(new Rect(x, y, sz, sz), tGlow, new Color(1f, .85f, .3f, .10f));
        }
        switch (page)
        {
            case Page.Main: MenuMain(); break;
            case Page.Select: MenuSelect(); break;
            case Page.Settings:
                Fill(new Rect(0, 0, UW, UH), new Color(0, 0, 0, .45f));
                if (DrawSettingsCard(new Rect(UW / 2 - 380, 40, 760, 640))) { SavePrefs(); SetPage(Page.Main); }
                break;
        }
    }

    void MenuMain()
    {
        Panel(new Rect(36, 30, 540, 660), new Color(.025f, .075f, .11f, .88f));
        float e0 = Ease(0), e1 = Ease(1), e2 = Ease(2);
        T(new Rect(84 - (1 - e0) * 70, 70, 460, 110), "SAND", S(104), new Color(cGold.r, cGold.g, cGold.b, e0));
        T(new Rect(84 - (1 - e1) * 70, 160, 460, 100), "CASTLE MAZE", S(68), new Color(1, 1, 1, e1));
        T(new Rect(88 - (1 - e2) * 70, 268, 460, 30), "Find the shells. Escape to the shore.", S(21, TextAnchor.MiddleLeft, false), new Color(cMuted.r, cMuted.g, cMuted.b, e2));

        string[] labels = { "PLAY", "SETTINGS", "QUIT" };
        for (int i = 0; i < labels.Length; i++)
        {
            float e = Ease(i + 2);
            if (Button(new Rect(84 - (1 - e) * 80, 340 + i * 88, 440, 66), labels[i], i == keyboardMenuChoice))
            {
                if (i == 0) SetPage(Page.Select);
                else if (i == 1) SetPage(Page.Settings);
                else Quit();
            }
        }
        T(new Rect(84, 645, 480, 26), "Tip: click PLAY, then choose a difficulty card", S(16, TextAnchor.MiddleLeft, false), cMuted);

        // best-score card
        Rect b = new Rect(UW - 360, UH - 190, 320, 150);
        Panel(b, new Color(.025f, .075f, .11f, .82f));
        T(new Rect(b.x + 24, b.y + 14, 280, 26), "BEST ESCAPES", S(16), cMuted);
        for (int i = 0; i < 3; i++)
        {
            var d = (Difficulty)i;
            T(new Rect(b.x + 24, b.y + 48 + i * 32, 140, 28), d.ToString(), S(21), i == 0 ? cTeal : i == 1 ? cGold : cRed);
            T(new Rect(b.x + 130, b.y + 48 + i * 32, 90, 28), PlayerPrefs.GetInt("mm_best_" + d, 0).ToString(), S(21, TextAnchor.MiddleRight), Color.white);
            int st = PlayerPrefs.GetInt("mm_stars_" + d, 0);
            for (int k = 0; k < 3; k++)
                Tex(new Rect(b.x + 232 + k * 26, b.y + 51 + i * 32, 22, 22), tStar, k < st ? cGold : new Color(1, 1, 1, .15f));
        }
    }

    void MenuSelect()
    {
        Fill(new Rect(0, 0, UW, UH), new Color(0, 0, 0, .45f));
        float e = Ease(0);
        T(new Rect(0, 30 - (1 - e) * 30, UW, 70), "CHOOSE YOUR BEACH CHALLENGE", S(48, TextAnchor.MiddleCenter), new Color(1, 1, 1, e));
        T(new Rect(0, 94, UW, 30), "PICK YOUR CASTLE CHALLENGE", S(19, TextAnchor.MiddleCenter, false), cMuted);
        float cw = 330, gap = 28, x0 = (UW - (3 * cw + 2 * gap)) / 2;
        for (int i = 0; i < 3; i++)
        {
            float ei = Ease(i + 1);
            DrawCard(new Rect(x0 + i * (cw + gap), 135 + (1 - ei) * 60, cw, 470), (Difficulty)i);
        }
        if (Button(new Rect(x0, 636, 200, 54), "< BACK")) SetPage(Page.Main);
    }

    void DrawCard(Rect r, Difficulty d)
    {
        var s = MakeSettings(d, 0);
        Color ac = d == Difficulty.Easy ? cTeal : d == Difficulty.Medium ? cGold : cRed;
        string desc = d == Difficulty.Easy ? "A small sandcastle, gentle patrols and helpful hints."
                    : d == Difficulty.Medium ? "A winding beach fort with more crabs and seagulls."
                    : "A sprawling fortress, sea urchins and a tight escape.";
        float h = Hover("card" + d, r.Contains(MouseGUI()));
        Rect rr = new Rect(r.x, r.y - h * 10f, r.width, r.height);
        Panel(rr, Color.Lerp(cCard, new Color(.075f, .20f, .26f, .98f), h));
        PanelS(new Rect(rr.x + 24, rr.y + 24, 56, 6), ac);
        T(new Rect(rr.x + 24, rr.y + 36, rr.width - 48, 50), d.ToString().ToUpper(), S(40), ac);
        T(new Rect(rr.x + 24, rr.y + 88, rr.width - 48, 56), desc, S(17, TextAnchor.UpperLeft, false, true), cMuted);

        string[,] rows =
        {
            { "Maze size", s.cells + " x " + s.cells },
            { "Seashells needed", s.cobsNeeded + " of " + s.cobsTotal },
            { "Crabs / Seagulls", s.dogs + " / " + s.crows },
            { "Sea urchins", s.traps.ToString() },
            { "Time limit", Clock(s.time) },
            { "Shore map + beacon", s.minimap ? "Yes" : "No" },
        };
        for (int i = 0; i < 6; i++)
        {
            float y = rr.y + 160 + i * 30;
            T(new Rect(rr.x + 24, y, 180, 28), rows[i, 0], S(18, TextAnchor.MiddleLeft, false), cMuted, false);
            T(new Rect(rr.x + 150, y, rr.width - 174, 28), rows[i, 1], S(18, TextAnchor.MiddleRight), Color.white, false);
        }
        int best = PlayerPrefs.GetInt("mm_best_" + d, 0), st = PlayerPrefs.GetInt("mm_stars_" + d, 0);
        T(new Rect(rr.x + 24, rr.y + 346, 190, 28), "Best: " + best, S(19), Color.white);
        for (int k = 0; k < 3; k++)
            Tex(new Rect(rr.xMax - 24 - 3 * 28 + k * 28, rr.y + 349, 24, 24), tStar, k < st ? cGold : new Color(1, 1, 1, .15f));

        // Native IMGUI hit target covers the complete card. The button uses
        // the stable unanimated rectangle while the artwork can float upward
        // on hover, so the clickable area never moves underneath the cursor.
        bool selected = GUI.Button(r, GUIContent.none, GUIStyle.none);

        Rect action = new Rect(rr.x + 24, rr.yMax - 76, rr.width - 48, 54);
        Panel(action, Color.Lerp(new Color(ac.r, ac.g, ac.b, .68f),
                                 new Color(ac.r, ac.g, ac.b, .95f), h));
        T(action, "START " + d.ToString().ToUpper() + " MAZE", S(21, TextAnchor.MiddleCenter),
          new Color(.07f, .13f, .16f), false);

        if (selected)
        {
            Play("click");
            GUI.FocusControl(null);
            StartNewGame(d);
        }
    }

    // ------------------------------------------------------------ SETTINGS
    bool DrawSettingsCard(Rect p)
    {
        Panel(p, cCard);
        T(new Rect(p.x + 40, p.y + 20, 500, 56), "BEACH SETTINGS", S(42), cGold);
        Fill(new Rect(p.x + 40, p.y + 78, p.width - 80, 2), new Color(cGold.r, cGold.g, cGold.b, .28f));
        float x = p.x + 40, w = p.width - 80, y = p.y + 92;

        SliderRow("Mouse sensitivity", ref sens, 0.2f, 3f, x, y, w, "0.0");
        SliderRow("Volume", ref volume, 0f, 1f, x, y + 64, w, "0%");
        SliderRow("Field of view", ref fovSet, 60f, 100f, x, y + 128, w, "0");

        musicOn = Toggle(new Rect(x, y + 196, w, 44), "Music", musicOn);
        bobOn = Toggle(new Rect(x, y + 246, w, 44), "Head bob", bobOn);
        mapOn = Toggle(new Rect(x, y + 296, w, 44), "Show minimap (Easy / Medium)", mapOn);

        T(new Rect(x, y + 352, w, 28), "WORLD THEME  (applies to the next maze)", S(16), cMuted);
        float cw = Mathf.Min(300f, w);
        for (int i = 0; i < Themes.Length; i++)
        {
            Rect themeRect = new Rect(x + i * (cw + 14), y + 384, cw, 50);
            if (Button(themeRect, Themes[i].name, i == themeIdx))
            {
                themeIdx = i;
                ApplyTheme();
                if (state == State.Menu) RebuildShowcase();
            }
            PanelS(new Rect(themeRect.x + 12, themeRect.y + 10, 6, themeRect.height - 20), Themes[i].w2);
        }
        return Button(new Rect(x, p.yMax - 82, 200, 54), "< BACK");
    }

    void SliderRow(string label, ref float v, float min, float max, float x, float y, float w, string fmt)
    {
        T(new Rect(x, y, w * 0.6f, 28), label, S(22), Color.white);
        string val = fmt == "0%" ? Mathf.RoundToInt(v * 100) + "%" : v.ToString(fmt);
        T(new Rect(x + w * 0.6f, y, w * 0.4f, 28), val, S(22, TextAnchor.MiddleRight), cGold);
        v = Slider(new Rect(x, y + 30, w, 30), v, min, max, "sl" + label);
    }

    // ==================================================================== HUD
    void DrawFx()
    {
        Tex(new Rect(0, 0, UW, UH), tVig, new Color(0, 0, 0, .38f));
        float a = 0f;
        if (state == State.Playing || state == State.Paused)
            a = Mathf.Clamp01(flash) * 0.8f + dangerSmooth * (0.30f + 0.12f * Mathf.Sin(uiClock * 9f));
        if (state == State.Lost) a = 0.55f;
        if (a > 0.01f) Tex(new Rect(0, 0, UW, UH), tVig, new Color(1f, .1f, .1f, Mathf.Clamp01(a)));
        if (state == State.Playing && levelTime < 0.9f) Fill(new Rect(0, 0, UW, UH), new Color(0, 0, 0, 1f - levelTime / 0.9f));
    }

    void DrawHUD()
    {
        // ---- time / score / lives
        Rect status = new Rect(18, 18, 312, 178);
        Panel(status, cCard);
        PanelS(new Rect(status.x + 12, status.y + 14, 4, status.height - 28), cGold);

        float frac = Mathf.Clamp01(timeLeft / cfg.time);
        Color tcol = Color.Lerp(cRed, cTeal, Mathf.Clamp01(frac * 2f));
        bool low = timeLeft < 30f;
        T(new Rect(42, 26, 140, 20), "TIME LEFT", S(14), cMuted);
        T(new Rect(42, 44, 160, 46), Clock(timeLeft), S(40), low
            ? Color.Lerp(cRed, Color.white, 0.5f + 0.5f * Mathf.Sin(uiClock * 8f)) : tcol);
        T(new Rect(190, 26, 124, 20), "SCORE", S(14, TextAnchor.MiddleRight), cMuted);
        T(new Rect(170, 44, 144, 46), score.ToString("N0"), S(34, TextAnchor.MiddleRight), Color.white);
        Bar(new Rect(42, 96, 270, 10), frac, tcol);
        T(new Rect(42, 108, 100, 18), "LIVES", S(12), cMuted, false);
        for (int i = 0; i < 5; i++)
        {
            Rect hr = new Rect(42 + i * 30, 128, 24, 22);
            Tex(hr, tHeart, i < lives ? cRed : new Color(1, 1, 1, .13f));
        }
        string lvl = extra > 0 ? "HARD +" + extra : diff.ToString().ToUpper();
        T(new Rect(42, 154, 270, 24), lvl + "  /  " + Themes[themeIdx].name.ToUpper(),
            S(14, TextAnchor.MiddleLeft, false), cMuted, false);

        // ---- castle quest / seashell collection progress
        Rect oc = new Rect(UW / 2 - 290, 16, 580, 86);
        Panel(oc, cCard);
        T(new Rect(oc.x, oc.y + 5, oc.width, 18), "CASTLE QUEST",
            S(13, TextAnchor.MiddleCenter), cMuted, false);
        string obj;
        if (gateOpen) obj = "CASTLE GATE OPEN - FOLLOW THE BEACON TO THE SHORE!";
        else if (diff == Difficulty.Hard) obj = "FOLLOW THE COMPASS TO THE EXIT - COLLECT SHELLS TO UNLOCK IT";
        else obj = "Collect seashells to open the castle gate";
        T(new Rect(oc.x + 12, oc.y + 23, oc.width - 24, 30), obj,
            S(diff == Difficulty.Hard ? 18 : 22, TextAnchor.MiddleCenter), gateOpen ? cTeal : cGold);
        float px0 = oc.center.x - (cfg.cobsNeeded * 26f) / 2f;
        for (int i = 0; i < cfg.cobsNeeded; i++)
            Tex(new Rect(px0 + i * 26, oc.y + 58, 19, 19), tStar,
                (gateOpen || i < cobs) ? cGold : new Color(1, 1, 1, .18f));

        // ---- beach report / pause guidance
        Rect field = new Rect(UW - 250, 18, 232, 154);
        Panel(field, cCard);
        T(new Rect(field.x + 18, field.y + 12, field.width - 36, 20), "BEACH REPORT", S(14), cMuted);
        T(new Rect(field.x + 18, field.y + 38, 92, 18), "SAND DOLLARS", S(12), cMuted, false);
        T(new Rect(field.x + 108, field.y + 34, 104, 24), coinsGot.ToString("N0"),
            S(21, TextAnchor.MiddleRight), cGold, false);
        T(new Rect(field.x + 18, field.y + 66, 92, 18), "CRABS / GULLS", S(12), cMuted, false);
        T(new Rect(field.x + 108, field.y + 62, 104, 24), (dogs.Count + crows.Count).ToString(),
            S(21, TextAnchor.MiddleRight), cRed, false);
        PanelM(new Rect(field.x + 18, field.y + 100, field.width - 36, 40),
            new Color(1f, 1f, 1f, .07f));
        T(new Rect(field.x + 18, field.y + 100, field.width - 36, 40),
            state == State.Playing ? "ESC  /  PAUSE" : "ESC  /  RESUME",
            S(15, TextAnchor.MiddleCenter), state == State.Playing ? cTeal : cMuted, false);

        // ---- direction arrow
        Vector3 tp;
        if (ArrowAllowed() && state == State.Playing && GetObjective(out tp))
        {
            Vector3 fwd = camT.forward; fwd.y = 0;
            Vector3 to = tp - player.position; to.y = 0;
            if (fwd.sqrMagnitude > 0.001f && to.sqrMagnitude > 0.001f)
            {
                float ang = Vector3.SignedAngle(fwd, to, Vector3.up);
                Vector2 ctr = new Vector2(UW / 2, 146);
                PanelM(new Rect(ctr.x - 48, ctr.y - 47, 96, 96),
                    new Color(.025f, .10f, .14f, diff == Difficulty.Hard ? .94f : .78f));
                PanelS(new Rect(ctr.x - 48, ctr.y - 47, 96, 4), diff == Difficulty.Hard ? cTeal : cGold);
                var m = GUI.matrix;
                GUIUtility.RotateAroundPivot(ang, ctr);
                Tex(new Rect(ctr.x - 27, ctr.y - 27, 54, 54), tArrow,
                    diff == Difficulty.Hard ? new Color(1f, .78f, .25f) : cGold);
                GUI.matrix = m;
                string targetLabel = diff == Difficulty.Hard || gateOpen ? "EXIT BEACON" : "OBJECTIVE";
                T(new Rect(ctr.x - 58, ctr.y + 25, 116, 18), targetLabel,
                    S(12, TextAnchor.MiddleCenter), cMuted, false);
                T(new Rect(ctr.x - 58, ctr.y + 42, 116, 18), Mathf.RoundToInt(to.magnitude) + " m",
                    S(15, TextAnchor.MiddleCenter), cGold, false);
            }
        }

        // ---- crosshair
        if (state == State.Playing)
        {
            Tex(new Rect(UW / 2 - 3, UH / 2 - 3, 6, 6), tCircle, new Color(1, 1, 1, .62f));
            Fill(new Rect(UW / 2 - 12, UH / 2 - 1, 7, 2), new Color(1, 1, 1, .35f));
            Fill(new Rect(UW / 2 + 5, UH / 2 - 1, 7, 2), new Color(1, 1, 1, .35f));
        }

        // ---- bottom-left: stamina, active power-ups and hint availability
        float by = UH - 40;
        if (speedT > 0) { Chip(new Rect(18, by, 220, 28), "CURRENT", speedT / SPEED_DUR, PKColor(PK.Speed)); by -= 36; }
        if (shieldT > 0) { Chip(new Rect(18, by, 220, 28), "SEA-GLASS", shieldT / SHIELD_DUR, PKColor(PK.Shield)); by -= 36; }
        string hs = hintT > 0 ? "ACTIVE " + Mathf.CeilToInt(hintT) + "s"
            : hintCd > 0 ? "WAIT " + Mathf.CeilToInt(hintCd) + "s"
            : "READY" + (HintCost() > 0 ? "  (-" + HintCost() + ")" : "");
        PanelM(new Rect(18, by, 220, 30), new Color(.025f, .09f, .13f, .84f));
        T(new Rect(30, by, 200, 30), "[H] TIDE TRAIL   " + hs, S(15),
            hintCd > 0 && hintT <= 0 ? cMuted : cTeal, false);

        if (stamina < 0.995f)
        {
            PanelM(new Rect(UW / 2 - 126, UH - 48, 252, 28), new Color(.025f, .09f, .13f, .82f));
            T(new Rect(UW / 2 - 116, UH - 47, 74, 26), "STAMINA", S(12), cMuted, false);
            Bar(new Rect(UW / 2 - 38, UH - 37, 154, 8), stamina, exhausted ? cRed : cTeal);
        }

        // ---- minimap frame
        if (cfg.minimap && mapOn)
        {
            Rect r = MiniRectPx();
            r = new Rect(r.x / uiS, r.y / uiS, r.width / uiS, r.height / uiS);
            Color g = cGold;
            Fill(new Rect(r.x - 3, r.y - 3, r.width + 6, 3), g);
            Fill(new Rect(r.x - 3, r.yMax, r.width + 6, 3), g);
            Fill(new Rect(r.x - 3, r.y, 3, r.height), g);
            Fill(new Rect(r.xMax, r.y, 3, r.height), g);
            T(new Rect(r.x, r.y - 30, r.width, 24), "SHORE MAP  [M]",
                S(14, TextAnchor.MiddleRight), cMuted);
        }

        // ---- key hints fade after the opening moments
        if (state == State.Playing && levelTime < 14f)
        {
            float al = Mathf.Clamp01((14f - levelTime) * 0.8f);
            T(new Rect(0, UH - 74, UW, 28),
                "WASD MOVE   /   SHIFT SPRINT   /   H HINT   /   M MAP   /   ESC PAUSE",
                S(16, TextAnchor.MiddleCenter, false), new Color(1, 1, 1, al * .82f));
        }
    }

    void Chip(Rect r, string label, float frac, Color c)
    {
        PanelM(r, new Color(0, 0, 0, .45f));
        T(new Rect(r.x + 12, r.y, 100, r.height), label, S(15), c, false);
        Bar(new Rect(r.x + 90, r.center.y - 4, r.width - 104, 8), frac, c);
    }

    void DrawToasts()
    {
        for (int i = 0; i < toasts.Count; i++)
        {
            var t = toasts[i];
            float age = t.max - t.life;
            float slide = (1f - Mathf.Clamp01(age * 5f)) * 80f;
            float al = Mathf.Clamp01(t.life * 3f);
            Rect r = new Rect(UW - 400 + slide, UH * 0.30f + i * 56, 380, 46);
            PanelM(r, new Color(.025f, .09f, .13f, .90f * al));
            PanelS(new Rect(r.x + 8, r.y + 8, 5, r.height - 16), new Color(t.col.r, t.col.g, t.col.b, al));
            T(new Rect(r.x + 24, r.y, r.width - 30, r.height), t.text, S(19), new Color(t.col.r, t.col.g, t.col.b, al), false);
        }
    }

    void DrawBanner()
    {
        if (bannerT <= 0) return;
        float al = Mathf.Clamp01(bannerT * 1.5f) * Mathf.Clamp01((3.5f - bannerT) * 2.5f);
        T(new Rect(0, UH * 0.26f, UW, 80), bannerTitle, S(66, TextAnchor.MiddleCenter), new Color(cGold.r, cGold.g, cGold.b, al));
        T(new Rect(0, UH * 0.26f + 76, UW, 40), bannerSub, S(24, TextAnchor.MiddleCenter, false), new Color(1, 1, 1, al));
    }

    // ================================================================ PAUSE
    void DrawPause()
    {
        Fill(new Rect(0, 0, UW, UH), new Color(0, 0, 0, .6f));
        if (pausePage == 1)
        {
            if (DrawSettingsCard(new Rect(UW / 2 - 380, 40, 760, 640))) { SavePrefs(); pausePage = 0; }
            return;
        }
        Rect c = new Rect(UW / 2 - 270, 90, 540, 540);
        Panel(c, cCard);
        T(new Rect(c.x, c.y + 20, c.width, 70), "BEACH BREAK", S(52, TextAnchor.MiddleCenter), Color.white);
        float x = c.x + 50, w = c.width - 100, y = c.y + 105;
        if (Button(new Rect(x, y, w, 58), "RESUME", true)) { state = State.Playing; Lock(); }
        if (Button(new Rect(x, y + 74, w, 58), "RESTART MAZE")) { score = levelStartScore; StartLevel(); }
        if (Button(new Rect(x, y + 148, w, 58), "SETTINGS")) pausePage = 1;
        if (Button(new Rect(x, y + 222, w, 58), "MAIN MENU")) ShowMenu();
        if (Button(new Rect(x, y + 296, w, 58), "QUIT")) Quit();
    }

    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ================================================================== END
    void DrawEnd(bool won)
    {
        Fill(new Rect(0, 0, UW, UH), new Color(0, 0, 0, .55f));
        Rect c = new Rect(UW / 2 - 300, 40, 600, 640);
        Panel(c, cCard);
        float pop = Mathf.Clamp01(endT * 4f);
        T(new Rect(c.x, c.y + 20 - (1 - pop) * 20, c.width, 70), won ? "CASTLE ESCAPED!" : "GAME OVER", S(52, TextAnchor.MiddleCenter), won ? cTeal : cRed);

        if (won)
        {
            for (int i = 0; i < 3; i++)
            {
                float p = Mathf.Clamp01((endT - 0.35f - i * 0.45f) * 3f);
                float sz = Mathf.Lerp(30, 84, p);
                float cx = c.center.x + (i - 1) * 100, cy = c.y + 150;
                bool earned = i < stars;
                Tex(new Rect(cx - sz / 2, cy - sz / 2, sz, sz), tStar, earned ? new Color(cGold.r, cGold.g, cGold.b, p) : new Color(1, 1, 1, .15f));
            }
            string[,] rows =
            {
                { "Time remaining", Clock(timeLeft) },
                { "Time bonus", "+" + timeBonus },
                { "Seashells", cobs + " / " + cfg.cobsTotal },
                { "Sand dollars", coinsGot.ToString() },
                { "Power-ups used", powerGot.ToString() },
                { "Flawless bonus", flawlessBonus > 0 ? "+" + flawlessBonus : "-" },
            };
            for (int i = 0; i < 6; i++)
            {
                float ra = Mathf.Clamp01((endT - 1.1f - i * 0.12f) * 4f);
                float y = c.y + 214 + i * 32;
                T(new Rect(c.x + 70, y, 260, 30), rows[i, 0], S(20, TextAnchor.MiddleLeft, false), new Color(cMuted.r, cMuted.g, cMuted.b, ra), false);
                T(new Rect(c.xMax - 250, y, 180, 30), rows[i, 1], S(20, TextAnchor.MiddleRight), new Color(1, 1, 1, ra), false);
            }
            float k = Mathf.Clamp01((endT - 1.9f) / 1.2f);
            int shown = Mathf.RoundToInt(Mathf.Lerp(levelStartScore, score, k));
            T(new Rect(c.x, c.y + 416, c.width, 22), "TOTAL SCORE", S(15, TextAnchor.MiddleCenter), cMuted, false);
            T(new Rect(c.x, c.y + 436, c.width, 60), shown.ToString(), S(52, TextAnchor.MiddleCenter), Color.white);
            if (newBest && endT > 3f)
                T(new Rect(c.x, c.y + 494, c.width, 28), "NEW BEST!", S(22, TextAnchor.MiddleCenter), Color.Lerp(cGold, Color.white, 0.5f + 0.5f * Mathf.Sin(uiClock * 6f)), false);

            float bx = c.x + 50, bw = c.width - 100, by = c.yMax - 128;
            string nxt = diff < Difficulty.Hard ? "NEXT MAZE  (" + (diff + 1) + ")" : "NEXT MAZE  (bigger!)";
            if (Button(new Rect(bx, by, bw, 54), nxt, true)) NextLevel();
            if (Button(new Rect(bx, by + 64, bw / 2 - 6, 50), "REPLAY")) { score = levelStartScore; StartLevel(); }
            if (Button(new Rect(bx + bw / 2 + 6, by + 64, bw / 2 - 6, 50), "MAIN MENU")) ShowMenu();
        }
        else
        {
            T(new Rect(c.x + 30, c.y + 120, c.width - 60, 40), endMsg, S(26, TextAnchor.MiddleCenter), Color.white);
            string[,] rows =
            {
                { "Seashells collected", cobs + " / " + cfg.cobsNeeded },
                { "Sand dollars", coinsGot.ToString() },
                { "Score", score.ToString() },
                { "Best score", PlayerPrefs.GetInt("mm_best_" + diff, 0).ToString() },
            };
            for (int i = 0; i < 4; i++)
            {
                float y = c.y + 200 + i * 38;
                T(new Rect(c.x + 70, y, 280, 34), rows[i, 0], S(21, TextAnchor.MiddleLeft, false), cMuted, false);
                T(new Rect(c.xMax - 250, y, 180, 34), rows[i, 1], S(21, TextAnchor.MiddleRight), Color.white, false);
            }
            float bx = c.x + 50, bw = c.width - 100, by = c.yMax - 190;
            if (Button(new Rect(bx, by, bw, 58), "TRY AGAIN", true)) { score = levelStartScore; StartLevel(); }
            if (Button(new Rect(bx, by + 72, bw, 54), "MAIN MENU")) ShowMenu();
        }
    }
}
