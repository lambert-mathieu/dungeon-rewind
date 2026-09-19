using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonRewind.Player {
    [RequireComponent(typeof(CharacterController))]
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
        private const float standingHeight = 2.0f;
        private const float crouchingHeight = standingHeight * crouchHeightScale;

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

        [SerializeField] private Transform cameraRoot;

        private CharacterController characterController;
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
            characterController = GetComponent<CharacterController>();
            characterController.height = standingHeight;
            characterController.center = new Vector3(0f, standingHeight / 2f, 0f);
            currentSpeed = groundSpeed;
        }

        private void Start() {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update() {
            float deltaTime = Time.deltaTime;
            bool hasMoveInput = moveInput.sqrMagnitude > 0.0001f;
            Vector3 inputDirection = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;

            HandleCursorToggle();
            ApplyLook();
            UpdateTimers(deltaTime);
            ApplyCrouch(deltaTime);
            ApplyVerticalMovement(deltaTime);
            ApplyHorizontalMovement(deltaTime, inputDirection, hasMoveInput);
            ApplyMotion(deltaTime);
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

        private void UpdateTimers(float deltaTime) {
            isGrounded = characterController.isGrounded;

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
            float previousCrouchProgression = crouchProgression;

            float progressionRate = isCrouchPressed ? (isGrounded ? crouchDownSpeed : airCrouchDownSpeed) : -(isGrounded ? crouchUpSpeed : airCrouchUpSpeed);

            crouchProgression = Mathf.Clamp01(crouchProgression + progressionRate * deltaTime);

            float height = Mathf.Lerp(standingHeight, crouchingHeight, crouchProgression);
            characterController.height = height;
            characterController.center = new Vector3(0f, height / 2f, 0f);

            if (isGrounded) {
                float heightDifference = standingHeight - crouchingHeight;
                transform.position -= new Vector3(0f, heightDifference * (crouchProgression - previousCrouchProgression), 0f);
            }
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

        private void ApplyMotion(float deltaTime) {
            Vector3 motion = new Vector3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.z);
            characterController.Move(motion * deltaTime);
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
