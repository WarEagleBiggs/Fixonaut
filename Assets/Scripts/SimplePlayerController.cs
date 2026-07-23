using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(BatterySystem))]
[RequireComponent(typeof(CrateInteraction))]
[RequireComponent(typeof(ObjectiveUI), typeof(RechargeStation))]
public class SimplePlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float turnSpeed = 12f;

    [Header("Camera")]
    [SerializeField] private Camera followCamera;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 3f, -5f);
    [Tooltip("Higher values mean less movement lag.")]
    [SerializeField] private float cameraMoveLerpSpeed = 10f;
    [Tooltip("Lower values make the camera lag farther behind turns.")]
    [SerializeField] private float cameraTurnLerpSpeed = 4f;
    [SerializeField] private float lookHeight = 1f;

    [Header("Tire Tracks")]
    [SerializeField] private float trackSpacing = 0.6f;
    [SerializeField] private float trackWidth = 0.12f;
    [SerializeField] private float trackGroundOffset = -0.98f;
    [SerializeField] private float trackDuration = 30f;
    [SerializeField] private Color trackColor = new Color(0.03f, 0.03f, 0.03f, 0.7f);

    private Rigidbody playerRigidbody;
    private Vector3 moveDirection;
    private Vector3 smoothedCameraTarget;
    private Quaternion smoothedCameraRotation;
    private TrailRenderer[] tireTracks;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
            return;

        if (GetComponent<BatterySystem>() == null
            || GetComponent<CrateInteraction>() == null
            || GetComponent<ObjectiveUI>() == null
            || GetComponent<RechargeStation>() == null)
            UnityEditor.EditorApplication.delayCall += AddRequiredComponentsInEditor;
    }

    private void AddRequiredComponentsInEditor()
    {
        if (this == null)
            return;

        if (GetComponent<BatterySystem>() == null)
            UnityEditor.Undo.AddComponent<BatterySystem>(gameObject);

        if (GetComponent<CrateInteraction>() == null)
            UnityEditor.Undo.AddComponent<CrateInteraction>(gameObject);

        if (GetComponent<ObjectiveUI>() == null)
            UnityEditor.Undo.AddComponent<ObjectiveUI>(gameObject);

        if (GetComponent<RechargeStation>() == null)
            UnityEditor.Undo.AddComponent<RechargeStation>(gameObject);
    }
#endif

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();

        if (playerRigidbody == null)
            playerRigidbody = gameObject.AddComponent<Rigidbody>();

        if (GetComponent<CapsuleCollider>() == null)
            gameObject.AddComponent<CapsuleCollider>();

        if (GetComponent<BatterySystem>() == null)
            gameObject.AddComponent<BatterySystem>();

        if (GetComponent<CrateInteraction>() == null)
            gameObject.AddComponent<CrateInteraction>();

        if (GetComponent<ObjectiveUI>() == null)
            gameObject.AddComponent<ObjectiveUI>();

        if (GetComponent<RechargeStation>() == null)
            gameObject.AddComponent<RechargeStation>();

        CharacterController oldController = GetComponent<CharacterController>();
        if (oldController != null)
            oldController.enabled = false;

        playerRigidbody.freezeRotation = false;
        playerRigidbody.constraints = RigidbodyConstraints.FreezeRotationX
            | RigidbodyConstraints.FreezeRotationZ;
        playerRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        playerRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

        if (followCamera == null)
            followCamera = Camera.main;

        CreateTireTracks();
    }

    private void Start()
    {
        if (followCamera == null)
            return;

        smoothedCameraTarget = transform.position;
        smoothedCameraRotation = transform.rotation;
        followCamera.transform.position = GetCameraPosition();
        LookCameraAtPlayer();
    }

    private void Update()
    {
        Vector2 input = ReadMoveInput();
        moveDirection = GetCameraRelativeDirection(input);
    }

    private void FixedUpdate()
    {
        Vector3 velocity = playerRigidbody.linearVelocity;
        velocity.x = moveDirection.x * moveSpeed;
        velocity.z = moveDirection.z * moveSpeed;
        playerRigidbody.linearVelocity = velocity;

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            Quaternion newRotation = Quaternion.Slerp(
                playerRigidbody.rotation,
                targetRotation,
                turnSpeed * Time.fixedDeltaTime);

            playerRigidbody.MoveRotation(newRotation);
        }
    }

    private void LateUpdate()
    {
        UpdateTireTracks();

        if (followCamera == null)
            return;

        float moveLerp = 1f - Mathf.Exp(-cameraMoveLerpSpeed * Time.deltaTime);
        float turnLerp = 1f - Mathf.Exp(-cameraTurnLerpSpeed * Time.deltaTime);

        smoothedCameraTarget = Vector3.Lerp(smoothedCameraTarget, transform.position, moveLerp);
        smoothedCameraRotation = Quaternion.Slerp(smoothedCameraRotation, transform.rotation, turnLerp);
        followCamera.transform.position = GetCameraPosition();

        LookCameraAtPlayer();
    }

    private Vector2 ReadMoveInput()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) input.y -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input.x += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input.x -= 1f;
        }

        if (Gamepad.current != null)
            input += Gamepad.current.leftStick.ReadValue();

        return Vector2.ClampMagnitude(input, 1f);
    }

    private Vector3 GetCameraRelativeDirection(Vector2 input)
    {
        if (followCamera == null)
            return new Vector3(input.x, 0f, input.y).normalized;

        Vector3 cameraForward = followCamera.transform.forward;
        Vector3 cameraRight = followCamera.transform.right;
        cameraForward.y = 0f;
        cameraRight.y = 0f;

        return (cameraForward.normalized * input.y + cameraRight.normalized * input.x).normalized;
    }

    private Vector3 GetCameraPosition()
    {
        return smoothedCameraTarget + smoothedCameraRotation * cameraOffset;
    }

    private void LookCameraAtPlayer()
    {
        followCamera.transform.LookAt(transform.position + Vector3.up * lookHeight);
    }

    private void CreateTireTracks()
    {
        tireTracks = new TrailRenderer[2];

        Shader trackShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (trackShader == null)
            trackShader = Shader.Find("Sprites/Default");

        Material trackMaterial = new Material(trackShader);
        trackMaterial.color = trackColor;

        for (int i = 0; i < tireTracks.Length; i++)
        {
            GameObject trackObject = new GameObject(i == 0 ? "Left Tire Track" : "Right Tire Track");
            trackObject.transform.SetParent(transform, false);

            float side = i == 0 ? -1f : 1f;
            trackObject.transform.localPosition = new Vector3(side * trackSpacing * 0.5f, trackGroundOffset, 0f);
            trackObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            TrailRenderer trail = trackObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = trackMaterial;
            trail.time = trackDuration;
            trail.startWidth = trackWidth;
            trail.endWidth = trackWidth;
            trail.minVertexDistance = 0.05f;
            trail.textureMode = LineTextureMode.Tile;
            trail.alignment = LineAlignment.TransformZ;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.emitting = false;

            tireTracks[i] = trail;
        }
    }

    private void UpdateTireTracks()
    {
        if (tireTracks == null)
            return;

        Vector3 horizontalVelocity = playerRigidbody.linearVelocity;
        horizontalVelocity.y = 0f;
        bool isMoving = horizontalVelocity.sqrMagnitude > 0.05f;

        foreach (TrailRenderer trail in tireTracks)
            trail.emitting = isMoving;
    }
}
