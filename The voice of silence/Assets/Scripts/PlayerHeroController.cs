using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerHeroController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float jumpHeight = 1.3f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;
    [SerializeField] private float headBobFrequency = 8f;
    [SerializeField] private float headBobAmplitude = 0.04f;
    [SerializeField] private float headBobSmoothing = 10f;
    [SerializeField] private float crouchHeight = 0.85f;
    [SerializeField] private float standingCameraY = 2f;
    [SerializeField] private float crouchingCameraY = 1.4f;
    [SerializeField] private float crouchStepOffset = 0.2f;
    [SerializeField] private float crouchTransitionSpeed = 10f;
    [SerializeField] private float groundProbeHeight = 5f;
    [SerializeField] private float groundProbeDistance = 50f;
    [SerializeField] private bool lockCursorOnStart = true;
    [SerializeField] private bool allowEscapeCursorToggle = true;

    private CharacterController controller;
    private float verticalVelocity;
    private float currentPitch;
    private float headBobTimer;
    private Vector3 standingCameraPivotLocalPosition;
    private float standingControllerHeight;
    private Vector3 standingControllerCenter;
    private float standingStepOffset;
    private bool isCrouching;
    private bool jumpRequested = false;
    private bool escapeToggleRequested = false;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraPivot == null)
        {
            Camera childCamera = GetComponentInChildren<Camera>();
            if (childCamera != null)
            {
                cameraPivot = childCamera.transform;
            }
        }
    }

    private void Start()
    {
        SnapToGround();
        if (cameraPivot != null)
        {
            currentPitch = NormalizeAngle(cameraPivot.localEulerAngles.x);
            standingCameraPivotLocalPosition = cameraPivot.localPosition;
            standingCameraPivotLocalPosition = new Vector3(
                standingCameraPivotLocalPosition.x,
                standingCameraY,
                standingCameraPivotLocalPosition.z);
            cameraPivot.localPosition = standingCameraPivotLocalPosition;
        }

        standingControllerHeight = controller.height;
        standingControllerCenter = controller.center;
        standingStepOffset = controller.stepOffset;

        if (lockCursorOnStart)
        {
            SetCursorLocked(true);
        }
    }

    private void FixedUpdate()
    {
        if (escapeToggleRequested)
        {
            SetCursorLocked(Cursor.lockState != CursorLockMode.Locked);
            escapeToggleRequested = false;
        }

        HandleMouseLook();

        KeyCode forwardKey = KeyBindManager.GetBoundKey("Forward", KeyCode.W);
        KeyCode backwardKey = KeyBindManager.GetBoundKey("Backward", KeyCode.S);
        KeyCode leftKey = KeyBindManager.GetBoundKey("Left", KeyCode.A);
        KeyCode rightKey = KeyBindManager.GetBoundKey("Right", KeyCode.D);
        KeyCode jumpKey = KeyBindManager.GetBoundKey("Jump", KeyCode.Space);
        bool sprintPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool crouchPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        Vector3 inputDirection = Vector3.zero;

        if (Input.GetKey(forwardKey))
        {
            inputDirection += Vector3.forward;
        }

        if (Input.GetKey(backwardKey))
        {
            inputDirection += Vector3.back;
        }

        if (Input.GetKey(leftKey))
        {
            inputDirection += Vector3.left;
        }

        if (Input.GetKey(rightKey))
        {
            inputDirection += Vector3.right;
        }

        if (inputDirection.sqrMagnitude > 1f)
        {
            inputDirection.Normalize();
        }

        if (crouchPressed)
        {
            isCrouching = true;
        }
        else if (isCrouching)
        {
            isCrouching = !CanStandUp();
        }

        bool isSprinting = sprintPressed && !isCrouching && controller.isGrounded && inputDirection.z > 0.1f;

        float currentSpeed = moveSpeed;
        if (isCrouching)
        {
            currentSpeed = crouchSpeed;
        }
        else if (isSprinting)
        {
            currentSpeed = sprintSpeed;
        }

        UpdateCrouchState();

        Vector3 horizontalMove = transform.TransformDirection(inputDirection) * currentSpeed;

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        if (controller.isGrounded && jumpRequested)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpRequested = false;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 move = new Vector3(horizontalMove.x, verticalVelocity, horizontalMove.z);
        controller.Move(move * Time.deltaTime);

        bool isMovingOnGround = controller.isGrounded && inputDirection.sqrMagnitude > 0.01f;
        ApplyHeadBob(isMovingOnGround);
    }

    private void HandleMouseLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        if (cameraPivot == null)
        {
            return;
        }

        currentPitch -= mouseY;
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
        cameraPivot.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
    }

    private void ApplyHeadBob(bool isMovingOnGround)
    {
        if (cameraPivot == null)
        {
            return;
        }

        float targetYOffset = 0f;

        if (isMovingOnGround)
        {
            headBobTimer += Time.deltaTime * headBobFrequency;
            targetYOffset = Mathf.Sin(headBobTimer) * headBobAmplitude;
        }
        else
        {
            headBobTimer = 0f;
        }

        float targetCameraY = isCrouching ? crouchingCameraY : standingCameraY;
        Vector3 targetLocalPosition = new Vector3(
            standingCameraPivotLocalPosition.x,
            Mathf.Max(0.1f, targetCameraY + targetYOffset),
            standingCameraPivotLocalPosition.z);
        cameraPivot.localPosition = Vector3.Lerp(cameraPivot.localPosition, targetLocalPosition, headBobSmoothing * Time.deltaTime);
    }

    private void UpdateCrouchState()
    {
        float crouchedHeight = Mathf.Clamp(crouchHeight, 0.5f, standingControllerHeight);
        float targetHeight = isCrouching ? crouchedHeight : standingControllerHeight;
        float newHeight = Mathf.Lerp(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);

        controller.height = newHeight;
        controller.center = new Vector3(standingControllerCenter.x, newHeight * 0.5f, standingControllerCenter.z);

        float targetStepOffset = isCrouching ? crouchStepOffset : standingStepOffset;
        float safeStepOffset = Mathf.Clamp(targetStepOffset, 0.05f, Mathf.Max(0.05f, controller.height - 0.05f));
        controller.stepOffset = safeStepOffset;
    }

    private bool CanStandUp()
    {
        float checkRadius = Mathf.Max(0.05f, controller.radius - 0.01f);
        float currentHalfHeight = controller.height * 0.5f;
        float standingHalfHeight = standingControllerHeight * 0.5f;
        float currentOffset = Mathf.Max(0f, currentHalfHeight - checkRadius);
        float standingOffset = Mathf.Max(0f, standingHalfHeight - checkRadius);

        Vector3 currentCenterWorld = transform.TransformPoint(controller.center);
        Vector3 standingCenterWorld = transform.TransformPoint(standingControllerCenter);
        Vector3 currentTop = currentCenterWorld + Vector3.up * currentOffset;
        Vector3 standingTop = standingCenterWorld + Vector3.up * standingOffset;

        float checkDistance = standingTop.y - currentTop.y;
        if (checkDistance <= 0f)
        {
            return true;
        }

        bool previousEnabled = controller.enabled;
        controller.enabled = false;
        bool blocked = Physics.SphereCast(currentTop, checkRadius, Vector3.up, out _, checkDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore);
        controller.enabled = previousEnabled;

        return !blocked;
    }

    private void SnapToGround()
    {
        Vector3 rayStart = transform.position + Vector3.up * groundProbeHeight;

        if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, groundProbeHeight + groundProbeDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        bool previousEnabled = controller.enabled;
        controller.enabled = false;

        float groundedY = hit.point.y + (controller.height * 0.5f) + controller.skinWidth;
        transform.position = new Vector3(transform.position.x, groundedY, transform.position.z);
        verticalVelocity = -2f;

        controller.enabled = previousEnabled;
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }

    public void SetCursorLocked(bool isLocked)
    {
        Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !isLocked;
    }

    private void OnGUI()
    {
        Event e = Event.current;
        if (e != null && e.type == EventType.KeyDown)
        {
            KeyCode jumpKey = KeyBindManager.GetBoundKey("Jump", KeyCode.Space);
            if (e.keyCode == jumpKey)
            {
                jumpRequested = true;
            }

            if (allowEscapeCursorToggle && e.keyCode == KeyCode.Escape)
            {
                escapeToggleRequested = true;
            }
        }
    }
}
