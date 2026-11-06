using UnityEngine;

public class NeonCameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 12f, -8f);
    public float pitchAngle = 55f;
    public float yawAngle = 0f;
    public float smoothSpeed = 8f;
    public float lookAheadDist = 1.6f;
    public float zoomMultiplier = 1f;

    private int presetIndex = 0;
    private Vector3 currentLookAhead;

    void Start()
    {
        ApplyPreset(0);
        SnapToTarget();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            presetIndex = (presetIndex + 1) % 3;
            ApplyPreset(presetIndex);
        }

        if (Input.GetKeyDown(KeyCode.Q)) yawAngle -= 45f;
        if (Input.GetKeyDown(KeyCode.E)) yawAngle += 45f;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            zoomMultiplier = Mathf.Clamp(zoomMultiplier - scroll * 0.6f, 0.65f, 1.55f);
        }
    }

    void LateUpdate()
    {
        if (!target)
        {
            NeonPlayer p = UnityEngine.Object.FindFirstObjectByType<NeonPlayer>();
            if (p) target = p.transform;
            if (!target) return;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = Quaternion.Euler(0f, yawAngle, 0f) * new Vector3(h, 0f, v);
        if (inputDir.sqrMagnitude > 1f) inputDir.Normalize();

        currentLookAhead = Vector3.Lerp(currentLookAhead, inputDir * lookAheadDist, Time.deltaTime * 5f);

        Quaternion yawRot = Quaternion.Euler(0f, yawAngle, 0f);
        Vector3 desiredPos = target.position + currentLookAhead + yawRot * (offset * zoomMultiplier);
        transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * smoothSpeed);

        Quaternion desiredRot = Quaternion.Euler(pitchAngle, yawAngle, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, Time.deltaTime * smoothSpeed);
    }

    public void ApplyPreset(int index)
    {
        presetIndex = index;
        if (presetIndex == 0)
        {
            offset = new Vector3(0f, 12f, -8f);
            pitchAngle = 55f;
            lookAheadDist = 1.6f;
        }
        else if (presetIndex == 1)
        {
            offset = new Vector3(0f, 20f, -5f);
            pitchAngle = 75f;
            lookAheadDist = 0.8f;
        }
        else
        {
            offset = new Vector3(0f, 7f, -7f);
            pitchAngle = 40f;
            lookAheadDist = 2.0f;
        }
    }

    public void SnapToTarget()
    {
        if (!target) return;
        Quaternion yawRot = Quaternion.Euler(0f, yawAngle, 0f);
        transform.position = target.position + yawRot * (offset * zoomMultiplier);
        transform.rotation = Quaternion.Euler(pitchAngle, yawAngle, 0f);
    }
}
