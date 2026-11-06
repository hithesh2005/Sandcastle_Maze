// MaizeMazeGame.PowerUps.cs  -  beach-themed power-up orbs, pickup effects and shell trail.
// To add a NEW power-up:  1) add a value to enum PK (MaizeMazeGame.cs)
//                         2) add its colour + name in PKColor / PKName below
//                         3) add a 'case' in Collect() below
using UnityEngine;
using System.Collections.Generic;

public partial class MaizeMazeGame
{
    float speedT, shieldT, hintT, hintCd;
    LineRenderer hintLine;
    const float SPEED_DUR = 8f, SHIELD_DUR = 15f;

    static Color PKColor(PK k)
    {
        switch (k)
        {
            case PK.Speed: return new Color(.20f, .80f, .94f);
            case PK.Shield: return new Color(.74f, .94f, 1f);
            case PK.Time: return new Color(1f, .78f, .32f);
            case PK.Heart: return new Color(.98f, .43f, .38f);
        }
        return Color.white;
    }

    // ---------------------------------------------------------- spawning
    void SpawnPowerUps(Transform root, System.Func<int, Vector2Int> take)
    {
        var kinds = new[] { PK.Speed, PK.Shield, PK.Time, PK.Heart };
        int off = Random.Range(0, kinds.Length);
        for (int i = 0; i < cfg.powerups; i++)
        {
            var t = take(3);
            AddOrb(root, T2W(t.x, t.y) + Vector3.up * 1f, kinds[(i + off) % kinds.Length]);
        }
    }

    void AddOrb(Transform root, Vector3 pos, PK k)
    {
        var p = new Pickup { k = k, phase = Random.value * 6.28f, basePos = pos };
        var g = new GameObject("BeachPowerUp_" + k).transform;
        g.SetParent(root, false); g.position = pos;
        Color c = PKColor(k);
        // A floating sea-glass pearl with a tiny five-armed starfish emblem.
        Prim(PrimitiveType.Sphere, g, Vector3.zero, Vector3.one * 0.44f, Mat(c, 1.25f), false);
        var pearl = Mat(new Color(1f, .96f, .84f), .75f);
        Prim(PrimitiveType.Sphere, g, new Vector3(-.09f, .10f, -.15f),
            new Vector3(.12f, .12f, .12f), pearl, false);
        for (int arm = 0; arm < 5; arm++)
        {
            float angle = arm * Mathf.PI * 2f / 5f;
            Vector3 direction = new Vector3(Mathf.Cos(angle), .16f, Mathf.Sin(angle));
            var ray = Prim(PrimitiveType.Capsule, g, direction * .12f,
                new Vector3(.07f, .18f, .07f), pearl, false);
            ray.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
        }
        p.t = g; pickups.Add(p);
    }

    // ------------------------------------------------------ collecting
    void Collect(Pickup p)
    {
        p.taken = true; p.t.gameObject.SetActive(false);
        switch (p.k)
        {
            case PK.Cob:
                cobs++; score += 100; Play("cob");
                if (cobs >= cfg.cobsNeeded && !gateOpen) OpenGate();
                else if (!gateOpen) Toast("Seashell  " + cobs + " / " + cfg.cobsNeeded, cGold, 1.6f);
                break;
            case PK.Coin:
                coinsGot++; score += 25; Play("coin");
                break;
            case PK.Speed:
                speedT = SPEED_DUR; powerGot++; Play("power");
                Toast("Ocean current boost!  " + (int)SPEED_DUR + "s", PKColor(p.k));
                break;
            case PK.Shield:
                shieldT = SHIELD_DUR; powerGot++; Play("power");
                Toast("Sea-glass shield ready - blocks the next hit", PKColor(p.k));
                break;
            case PK.Time:
                timeLeft += 20f; powerGot++; Play("power");
                Toast("+20 seconds!", PKColor(p.k));
                break;
            case PK.Heart:
                powerGot++; Play("power");
                if (lives < 5) { lives++; Toast("+1 life", PKColor(p.k)); }
                else { score += 50; Toast("Lives full - +50 pts", PKColor(p.k)); }
                break;
        }
    }

    void UpdatePower(float dt)
    {
        if (speedT > 0) speedT -= dt;
        if (shieldT > 0) shieldT -= dt;
        if (hintCd > 0) hintCd -= dt;
        if (hintT > 0)
        {
            hintT -= dt;
            if (hintT <= 0 && hintLine) hintLine.enabled = false;
            else if (hintLine)
            {
                var c = new Color(.30f, .92f, 1f, 0.55f + 0.4f * Mathf.Sin(Time.time * 6f));
                hintLine.startColor = c; hintLine.endColor = c;
            }
        }
    }

    // ------------------------------------------------------ hint path
    void InitHint()
    {
        var g = new GameObject("HintPath");
        hintLine = g.AddComponent<LineRenderer>();
        hintLine.material = MazeVisuals.Unlit(Color.white);
        hintLine.widthMultiplier = 0.35f;
        hintLine.alignment = LineAlignment.TransformZ;
        hintLine.numCornerVertices = 3;
        hintLine.useWorldSpace = true;
        hintLine.positionCount = 0;
        hintLine.enabled = false;
        g.transform.rotation = Quaternion.Euler(90, 0, 0);
    }

    int HintCost() { return (diff == Difficulty.Easy && extra == 0) ? 0 : (diff == Difficulty.Medium ? 25 : 75); }

    void TryHint()
    {
        if (hintCd > 0) { Toast("Tide compass recharging... " + Mathf.CeilToInt(hintCd) + "s", cMuted, 1.5f); return; }
        int cost = HintCost();
        if (score < cost) { Toast("The tide compass needs " + cost + " points", cRed, 1.8f); return; }
        score -= cost; hintT = 8f; hintCd = 25f;
        Play("hint");
        Toast(cost > 0 ? "Tide trail!  -" + cost + " pts" : "Tide trail revealed!", cTeal);
        RefreshHint();
    }

    void RefreshHint()
    {
        if (dist == null || hintLine == null) return;
        Vector2Int goal = new Vector2Int(-1, -1);
        bool toGate = gateOpen;
        if (!gateOpen)
        {
            int best = 99999;
            foreach (var p in pickups)
            {
                if (p.taken || p.k != PK.Cob) continue;
                var t = W2T(p.basePos);
                if (!Open(t.x, t.y)) continue;
                if (dist[t.x, t.y] < best) { best = dist[t.x, t.y]; goal = t; }
            }
            if (goal.x < 0) toGate = true;
        }
        if (toGate) goal = new Vector2Int(n - 2, n - 2);
        if (!Open(goal.x, goal.y) || dist[goal.x, goal.y] >= 9999) { hintLine.enabled = false; return; }

        var pts = new List<Vector3>();
        Vector3 up = Vector3.up * 0.08f;
        if (toGate) pts.Add(gatePos + up);
        var cur = goal; int guard = 0;
        while (dist[cur.x, cur.y] > 0 && guard++ < 3000)
        {
            pts.Add(T2W(cur.x, cur.y) + up);
            var next = cur;
            foreach (var d in D4)
            {
                var q = cur + d;
                if (Open(q.x, q.y) && dist[q.x, q.y] == dist[cur.x, cur.y] - 1) { next = q; break; }
            }
            if (next == cur) break;
            cur = next;
        }
        pts.Add(new Vector3(player.position.x, 0.08f, player.position.z));
        hintLine.positionCount = pts.Count;
        hintLine.SetPositions(pts.ToArray());
        hintLine.enabled = true;
    }

    // ---------------------------------------- objective (for the HUD arrow)
    bool GetObjective(out Vector3 pos)
    {
        pos = gatePos;
        // In Hard mode the compass always marks the final exit, even while
        // the castle gate is sealed. Collect the required shells on the way.
        if (diff == Difficulty.Hard || gateOpen) return true;
        float best = 1e9f; bool found = false;
        foreach (var p in pickups)
        {
            if (p.taken || p.k != PK.Cob) continue;
            Vector2Int tile = W2T(p.basePos);
            float d = Flat(p.basePos, player.position);
            // dist is a BFS distance through open maze cells. Use it when
            // available so the compass favours a reachable shell, not one
            // that merely looks close through a wall.
            if (dist != null && Open(tile.x, tile.y) && dist[tile.x, tile.y] < 9999)
                d = dist[tile.x, tile.y] * CS;
            if (d < best) { best = d; pos = p.basePos; found = true; }
        }
        return found;
    }

    // Easy: arrow always. Medium: arrow once the gate is open.
    // Hard: always point to the final exit beacon, including while locked.
    bool ArrowAllowed()
    {
        if (diff == Difficulty.Hard) return true;
        if (diff == Difficulty.Easy && extra == 0) return true;
        if (diff == Difficulty.Medium && extra == 0) return gateOpen;
        return false;
    }
}
