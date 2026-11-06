using UnityEngine;

public class NeonExit : MonoBehaviour
{
    public bool isUnlocked = false;
    public Transform barrierDoor;
    public Renderer[] gateRenderers;
    public Color lockedColor = new Color(0.92f, 0.25f, 0.25f);
    public Color unlockedColor = new Color(0.18f, 0.86f, 0.38f);

    private Vector3 closedLocalPos;
    private Vector3 openLocalPos;

    void Start()
    {
        if (barrierDoor)
        {
            closedLocalPos = barrierDoor.localPosition;
            openLocalPos = closedLocalPos + new Vector3(0f, -2.5f, 0f);
        }
        RefreshGateColors();
    }

    void Update()
    {
        if (barrierDoor)
        {
            Vector3 target = isUnlocked ? openLocalPos : closedLocalPos;
            barrierDoor.localPosition = Vector3.Lerp(barrierDoor.localPosition, target, Time.deltaTime * 5f);
        }
    }

    public void UnlockGate()
    {
        isUnlocked = true;
        if (barrierDoor)
        {
            Collider c = barrierDoor.GetComponent<Collider>();
            if (c) c.enabled = false;
        }
        RefreshGateColors();
    }

    void RefreshGateColors()
    {
        if (gateRenderers == null) return;
        Color col = isUnlocked ? unlockedColor : lockedColor;
        foreach (Renderer r in gateRenderers)
        {
            if (r && r.sharedMaterial)
            {
                if (Application.isPlaying)
                {
                    r.material.color = col;
                    if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", col);
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other) return;
        NeonPlayer player = other.GetComponent<NeonPlayer>();
        if (!player) player = other.GetComponentInParent<NeonPlayer>();
        if (!player || !NeonMazeGame.Instance) return;

        if (isUnlocked || NeonMazeGame.Instance.hasKey)
        {
            NeonMazeGame.Instance.Finish(true, "LEVEL " + NeonMazeGame.currentLevel + " COMPLETED!");
        }
        else
        {
            NeonMazeGame.Instance.ShowTemporaryMessage("EXIT GATE LOCKED! Find the GOLDEN KEY inside the maze first!");
        }
    }
}
