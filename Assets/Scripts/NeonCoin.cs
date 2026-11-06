using UnityEngine;

public class NeonCoin : MonoBehaviour
{
    public enum ItemKind
    {
        Coin = 0,
        GoldenKey = 1,
        SpeedBoostPad = 2
    }

    public ItemKind itemKind = ItemKind.Coin;
    public float spinSpeed = 110f;
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        if (itemKind == ItemKind.SpeedBoostPad) return;

        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        float bob = Mathf.Sin(Time.time * 3.2f) * 0.12f;
        transform.position = new Vector3(startPos.x, startPos.y + bob, startPos.z);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other) return;
        NeonPlayer player = other.GetComponent<NeonPlayer>();
        if (!player) player = other.GetComponentInParent<NeonPlayer>();
        if (!player) return;

        if (itemKind == ItemKind.GoldenKey)
        {
            if (NeonMazeGame.Instance) NeonMazeGame.Instance.CollectKey();
            Destroy(gameObject);
        }
        else if (itemKind == ItemKind.Coin)
        {
            if (NeonMazeGame.Instance) NeonMazeGame.Instance.AddCoin();
            Destroy(gameObject);
        }
        else if (itemKind == ItemKind.SpeedBoostPad)
        {
            player.ActivateSpeedBoost(1.6f, 3.0f);
            if (NeonMazeGame.Instance) NeonMazeGame.Instance.ShowTemporaryMessage("SPEED BOOST! Zooming through the maze!");
        }
    }
}
