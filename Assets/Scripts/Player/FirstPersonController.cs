using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonRewind.Player {
    [RequireComponent(typeof(Rigidbody))]
    public class FirstPersonController : MonoBehaviour {
        private const float groundSpeed = 7.4f;
        private const float sprintSpeed = 10.0f;
        private const float crouchSpeed = 5.1f;
        private const float groundAcceleration = 100.0f;
        private const float groundDeceleration = 60.0f;
        private const float crouchAcceleration = 60.0f;
        private const float crouchDeceleration = 40.0f;
        private const float airAcceleration = 50.0f;
        private const float airDeceleration = 15.0f;
        private const float speedTransitionSpeed = 8.0f;
        private const float speedTransitionTime = 2.0f;

        private const float crouchHeightScale = 0.65f;
        private const float crouchDownSpeed = 4.5f;
        private const float crouchUpSpeed = 4.1f;
        private const float airCrouchDownSpeed = 6.1f;
        private const float airCrouchUpSpeed = 12.0f;

        private const float gravity = -16.0f;
        private const float coyoteGravity = -12.0f;
        private const float maxFallSpeed = 34.0f;
        private const float jumpVelocity = 6.1f;
        private const float jumpVelocitySustain = 5.5f;
        private const float crouchJumpVelocity = 5.1f;
        private const float crouchJumpVelocitySustain = 4.5f;
        private const float jumpCooldownTime = 0.04f;
        private const float jumpCoyoteTime = 0.09f;
        private const float jumpBufferTime = 0.09f;
        private const float jumpMinHoldTime = 0.0f;
        private const float jumpMaxHoldTime = 0.2f;
        private const float jumpApexFallBonusGravity = -11.5f;
        private const float jumpApexFallBonusTime = 0.25f;

        private const float mouseSensitivity = 0.12f;
        private const float minPitch = -87.0f;
        private const float maxPitch = 87.0f;

        private const float playerRadius = 0.45f;
        private const float groundCheckRadius = playerRadius * 0.9f;
        private const float groundCheckDistance = groundCheckRadius + 0.15f;
        private const float slopeLimit = 45.0f;

        [SerializeField] private Transform cameraRoot;
        [SerializeField] private Transform playerScale;

        private Rigidbody rb;
        private LayerMask groundCheckMask;
        private Vector2 moveInput;
        private Vector2 lookInput;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private float pitch;

        private bool isSprinting;
        private bool isCrouchPressed;
        private bool isJumpHeld;
        private bool jumpPressedThisFrame;
        private bool jumpReleasedThisFrame;

        private bool isGrounded;
        private float crouchProgression;
        private float currentSpeed;
        private float timeSinceGrounded = 99f;
        private float coyoteTimer = 99f;
        private float lastJumpPressedTime = 99f;
        private float lastJumpTime = 99f;
        private float lastJumpApexTime = 99f;
        private bool isJumping;
        private bool isJumpSustainReleased;
        private float currentJumpVelocitySustain;

        private void Awake() {
            rb = GetComponent<Rigidbody>();
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.useGravity = false;
            groundCheckMask = ~(1 << gameObject.layer);

            playerScale.localScale = Vector3.one;

            currentSpeed = groundSpeed;
        }

        private void Start() {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update() {
            HandleCursorToggle();
            ApplyLook();
        }

        private void FixedUpdate() {
            float deltaTime = Time.fixedDeltaTime;
            bool hasMoveInput = moveInput.sqrMagnitude > 0.0001f;
            Vector3 inputDirection = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;

            UpdateTimers(deltaTime);
            ApplyCrouch(deltaTime);
            ApplyVerticalMovement(deltaTime);
            ApplyHorizontalMovement(deltaTime, inputDirection, hasMoveInput);
            ApplyMotion();
            ConsumeFrameInputFlags();
        }

        private void HandleCursorToggle() {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) {
                return;
            }

            bool isCurrentlyLocked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = isCurrentlyLocked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isCurrentlyLocked;
        }

        private void ApplyLook() {
            if (Cursor.lockState != CursorLockMode.Locked) {
                return;
            }

            float yawDelta = lookInput.x * mouseSensitivity;
            pitch = Mathf.Clamp(pitch - lookInput.y * mouseSensitivity, minPitch, maxPitch);

            transform.Rotate(Vector3.up * yawDelta);
            cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private bool GroundCheck(out Vector3 groundNormal) {
            Vector3 origin = transform.position + Vector3.up * groundCheckRadius;
            bool didHit = Physics.SphereCast(origin, groundCheckRadius, Vector3.down, out RaycastHit hit, groundCheckDistance, groundCheckMask, QueryTriggerInteraction.Ignore);
            groundNormal = didHit ? hit.normal : Vector3.up;
            return didHit && Vector3.Angle(hit.normal, Vector3.up) <= slopeLimit;
        }

        private void UpdateTimers(float deltaTime) {
            isGrounded = GroundCheck(out _);

            lastJumpTime += deltaTime;

            if (isGrounded || isJumping) {
                lastJumpApexTime = 99f;
            } else {
                lastJumpApexTime += deltaTime;
            }

            if (isGrounded) {
                timeSinceGrounded = 0f;
                coyoteTimer = 0f;
            } else {
                timeSinceGrounded += deltaTime;
                coyoteTimer += deltaTime;
            }

            if (jumpPressedThisFrame) {
                lastJumpPressedTime = 0f;
            } else {
                lastJumpPressedTime += deltaTime;
            }
        }

        private void ApplyCrouch(float deltaTime) {
            float progressionRate = isCrouchPressed ? (isGrounded ? crouchDownSpeed : airCrouchDownSpeed) : -(isGrounded ? crouchUpSpeed : airCrouchUpSpeed);

            crouchProgression = Mathf.Clamp01(crouchProgression + progressionRate * deltaTime);

            float heightScale = Mathf.Lerp(1f, crouchHeightScale, crouchProgression);
            Vector3 scale = playerScale.localScale;
            scale.y = heightScale;
            playerScale.localScale = scale;
        }

        private void ApplyVerticalMovement(float deltaTime) {
            if (!isGrounded) {
                verticalVelocity += (coyoteTimer <= jumpCoyoteTime ? coyoteGravity : gravity) * deltaTime;
            }

            if (lastJumpApexTime <= jumpApexFallBonusTime) {
                verticalVelocity += jumpApexFallBonusGravity * deltaTime;
            }

            if (isJumping) {
                if (jumpReleasedThisFrame || !isJumpHeld) {
                    isJumpSustainReleased = true;
                }

                bool shouldEndSustain = (isJumpSustainReleased && lastJumpTime >= jumpMinHoldTime)
                    || lastJumpTime >= jumpMaxHoldTime
                    || verticalVelocity <= 1f
                    || isGrounded;

                if (shouldEndSustain) {
                    isJumping = false;
                    lastJumpApexTime = 0f;
                } else {
                    verticalVelocity += currentJumpVelocitySustain * deltaTime;
                }
            } else {
                bool canJumpNow = lastJumpTime >= jumpCooldownTime && (
                    (jumpPressedThisFrame && isGrounded)
                    || (jumpPressedThisFrame && coyoteTimer <= jumpCoyoteTime)
                    || (lastJumpPressedTime <= jumpBufferTime && isGrounded)
                );

                if (canJumpNow) {
                    verticalVelocity = Mathf.Lerp(jumpVelocity, crouchJumpVelocity, crouchProgression);
                    currentJumpVelocitySustain = Mathf.Lerp(jumpVelocitySustain, crouchJumpVelocitySustain, crouchProgression);

                    lastJumpPressedTime = 99f;
                    coyoteTimer = 99f;
                    lastJumpTime = 0f;
                    isJumping = true;
                    isJumpSustainReleased = jumpReleasedThisFrame || !isJumpHeld;
                }
            }

            verticalVelocity = Mathf.Clamp(verticalVelocity, -maxFallSpeed, maxFallSpeed);
        }

        private void ApplyHorizontalMovement(float deltaTime, Vector3 inputDirection, bool hasMoveInput) {
            if (isGrounded) {
                float baseSpeed = isSprinting ? sprintSpeed : groundSpeed;
                currentSpeed = Mathf.Lerp(baseSpeed, crouchSpeed, crouchProgression);
            } else if (timeSinceGrounded >= speedTransitionTime) {
                currentSpeed = Mathf.Clamp(currentSpeed + speedTransitionSpeed * deltaTime, crouchSpeed, groundSpeed);
            }

            float acceleration = hasMoveInput
                ? (isGrounded ? Mathf.Lerp(groundAcceleration, crouchAcceleration, crouchProgression) : airAcceleration)
                : (isGrounded ? Mathf.Lerp(groundDeceleration, crouchDeceleration, crouchProgression) : airDeceleration);

            Vector3 targetVelocity = inputDirection * currentSpeed;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, acceleration * deltaTime);
        }

        private void ApplyMotion() {
            rb.linearVelocity = new Vector3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.z);
        }

        private void ConsumeFrameInputFlags() {
            jumpPressedThisFrame = false;
            jumpReleasedThisFrame = false;
        }

        public void OnMove(InputValue value) {
            moveInput = Vector2.ClampMagnitude(value.Get<Vector2>(), 1f);
        }

        public void OnLook(InputValue value) {
            lookInput = value.Get<Vector2>();
        }

        public void OnJump(InputValue value) {
            isJumpHeld = value.isPressed;

            if (isJumpHeld) {
                jumpPressedThisFrame = true;
            } else {
                jumpReleasedThisFrame = true;
            }
        }

        public void OnSprint(InputValue value) {
            isSprinting = value.isPressed;
        }

        public void OnCrouch(InputValue value) {
            isCrouchPressed = value.isPressed;
        }
    }
}
