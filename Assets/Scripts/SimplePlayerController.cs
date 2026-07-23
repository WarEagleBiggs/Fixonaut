using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
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

    private Rigidbody playerRigidbody;
    private Vector3 moveDirection;
    private Vector3 smoothedCameraTarget;
    private Quaternion smoothedCameraRotation;

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();

        if (playerRigidbody == null)
            playerRigidbody = gameObject.AddComponent<Rigidbody>();

        if (GetComponent<CapsuleCollider>() == null)
            gameObject.AddComponent<CapsuleCollider>();

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
}
