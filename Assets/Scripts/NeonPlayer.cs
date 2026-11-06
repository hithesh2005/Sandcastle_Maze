using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class NeonPlayer : MonoBehaviour
{
    public float walkSpeed = 6.5f;
    public float sprintSpeed = 9.5f;
    public float jumpHeight = 1.2f;
    public float gravity = -22f;
    public float mouseSensitivity = 2.2f;
    public Transform cameraPivot;
    public bool useDogCharacter = true;

    public Transform visualRoot;
    public Transform dogGroup;
    public Transform humanGroup;
    public Transform compassArrow;

    public Transform[] dogLegs;
    public Transform dogTail;
    public Transform[] humanLimbs;

    private CharacterController controller;
    private NeonCameraFollow camFollow;
    private Vector3 velocity;
    private float walkAnimTimer;
    private float speedBoostTimer;
    private float speedBoostMultiplier = 1f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        camFollow = UnityEngine.Object.FindFirstObjectByType<NeonCameraFollow>();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (!visualRoot) visualRoot = transform.Find("CharacterVisualRoot");
        if (!dogGroup && visualRoot) dogGroup = visualRoot.Find("DogModel");
        if (!humanGroup && visualRoot) humanGroup = visualRoot.Find("HumanModel");
        if (!compassArrow) compassArrow = transform.Find("CompassArrow");

        RefreshCharacterModel();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            useDogCharacter = !useDogCharacter;
            RefreshCharacterModel();
        }

        if (speedBoostTimer > 0f)
        {
            speedBoostTimer -= Time.deltaTime;
            if (speedBoostTimer <= 0f) speedBoostMultiplier = 1f;
        }

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(x, 0f, z);
        if (input.sqrMagnitude > 1f) input.Normalize();

        float camYaw = camFollow ? camFollow.yawAngle : 0f;
        Vector3 move = Quaternion.Euler(0f, camYaw, 0f) * input;

        float baseSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;
        float finalSpeed = baseSpeed * speedBoostMultiplier;
        if (controller && controller.enabled)
        {
            controller.Move(move * finalSpeed * Time.deltaTime);
        }

        if (move.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(move, Vector3.up);
            if (visualRoot)
                visualRoot.rotation = Quaternion.Slerp(visualRoot.rotation, targetRot, Time.deltaTime * 14f);
            else
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 14f);
        }

        if (controller != null && controller.enabled)
        {
            if (controller.isGrounded && velocity.y < 0f) velocity.y = -2f;
            if (controller.isGrounded && Input.GetKeyDown(KeyCode.Space))
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }

        AnimateLimbs(input.magnitude);
        UpdateObjectiveCompass();
    }

    public void ActivateSpeedBoost(float multiplier, float duration)
    {
        speedBoostMultiplier = multiplier;
        speedBoostTimer = duration;
    }

    public void RefreshCharacterModel()
    {
        if (dogGroup) dogGroup.gameObject.SetActive(useDogCharacter);
        if (humanGroup) humanGroup.gameObject.SetActive(!useDogCharacter);
    }

    void AnimateLimbs(float moveAmount)
    {
        if (moveAmount > 0.05f)
            walkAnimTimer += Time.deltaTime * 14f * speedBoostMultiplier;
        else
            walkAnimTimer = Mathf.Lerp(walkAnimTimer, 0f, Time.deltaTime * 8f);

        float swing = Mathf.Sin(walkAnimTimer) * 30f * moveAmount;

        if (dogLegs != null && dogLegs.Length >= 4)
        {
            if (dogLegs[0]) dogLegs[0].localRotation = Quaternion.Euler(swing, 0f, 0f);
            if (dogLegs[1]) dogLegs[1].localRotation = Quaternion.Euler(-swing, 0f, 0f);
            if (dogLegs[2]) dogLegs[2].localRotation = Quaternion.Euler(-swing, 0f, 0f);
            if (dogLegs[3]) dogLegs[3].localRotation = Quaternion.Euler(swing, 0f, 0f);
        }

        if (dogTail)
        {
            bool hasKey = NeonMazeGame.Instance && NeonMazeGame.Instance.hasKey;
            float wagSpeed = hasKey ? 18f : 11f;
            float wag = Mathf.Sin(Time.time * wagSpeed) * 28f;
            dogTail.localRotation = Quaternion.Euler(-30f, wag, 0f);
        }

        if (humanLimbs != null && humanLimbs.Length >= 4)
        {
            if (humanLimbs[0]) humanLimbs[0].localRotation = Quaternion.Euler(swing, 0f, 0f);
            if (humanLimbs[1]) humanLimbs[1].localRotation = Quaternion.Euler(-swing, 0f, 0f);
            if (humanLimbs[2]) humanLimbs[2].localRotation = Quaternion.Euler(-swing * 0.8f, 0f, 0f);
            if (humanLimbs[3]) humanLimbs[3].localRotation = Quaternion.Euler(swing * 0.8f, 0f, 0f);
        }
    }

    void UpdateObjectiveCompass()
    {
        if (!compassArrow || !NeonMazeGame.Instance) return;
        Transform target = NeonMazeGame.Instance.GetObjectiveTarget();
        if (!target)
        {
            compassArrow.gameObject.SetActive(false);
            return;
        }

        compassArrow.gameObject.SetActive(true);
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.05f)
        {
            compassArrow.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }
    }
}
