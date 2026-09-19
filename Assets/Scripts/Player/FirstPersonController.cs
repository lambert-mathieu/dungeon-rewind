using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonRewind.Player {
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour {
        private const float groundSpeed = 6f;
        private const float sprintSpeed = 8.5f;
        private const float crouchSpeed = 3f;
        private const float groundAcceleration = 140f;
        private const float groundDeceleration = 120f;
        private const float crouchAcceleration = 70f;
        private const float crouchDeceleration = 55f;
        private const float airAcceleration = 18f;
        private const float jumpControlAcceleration = 95f;
        private const float jumpControlWindowDuration = 0.25f;
        private const float airDeceleration = 7f;

        private const float slideMinSpeed = 4f;
        private const float slideBoostAmount = 3f;
        private const float slideMaxSpeed = 15f;
        private const float slideFriction = 5f;

        private const float crouchHeightScale = 0.7f;
        private const float crouchDownSpeed = 4.5f;
        private const float crouchUpSpeed = 4.1f;
        private const float airCrouchDownSpeed = 6.1f;
        private const float airCrouchUpSpeed = 12f;
        private const float standingHeight = 1.8f;
        private const float crouchingHeight = standingHeight * crouchHeightScale;

        private const float gravity = -16f;
        private const float coyoteGravity = -12f;
        private const float maxFallSpeed = 34f;
        private const float jumpVelocity = 6.1f;
        private const float jumpVelocitySustain = 5.5f;
        private const float crouchJumpVelocity = 4.1f;
        private const float crouchJumpVelocitySustain = 1.5f;
        private const float jumpCooldownTime = 0.04f;
        private const float jumpCoyoteTime = 0.09f;
        private const float jumpBufferTime = 0.09f;
        private const float jumpMinHoldTime = 0f;
        private const float jumpMaxHoldTime = 0.2f;
        private const float jumpApexFallBonusGravity = -11.5f;
        private const float jumpApexFallBonusTime = 0.25f;

        private const float mouseSensitivity = 0.12f;
        private const float minPitch = -85f;
        private const float maxPitch = 85f;

        private const float headBobKickScale = 0.1f;
        private const float headBobSpringStiffness = 180f;
        private const float headBobSpringDamping = 26.8f;
        private const float headBobMaxOffset = 0.5f;

        private const float fovBoostAmount = 4f;
        private const float fovChangeSpeed = 12f;

        [SerializeField] private Transform cameraRoot;
        [SerializeField] private Camera playerCamera;

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
        private bool isSliding;
        private float crouchProgression;
        private float timeSinceGrounded = 99f;
        private float lastJumpPressedTime = 99f;
        private float lastJumpTime = 99f;
        private float lastJumpApexTime = 99f;
        private float timeSinceJumpTriggered = 99f;
        private bool isJumping;
        private bool isJumpSustainReleased;
        private float currentJumpVelocitySustain;

        private Vector3 standingCameraLocalPosition;
        private Vector3 crouchingCameraLocalPosition;
        private float headBobOffset;
        private float headBobVelocity;
        private float baseFov;

        private void Awake() {
            characterController = GetComponent<CharacterController>();
            characterController.height = standingHeight;
            characterController.center = new Vector3(0f, standingHeight / 2f, 0f);

            standingCameraLocalPosition = cameraRoot.localPosition;
            float heightDifference = standingHeight - crouchingHeight;
            crouchingCameraLocalPosition = standingCameraLocalPosition - new Vector3(0f, heightDifference, 0f);

            baseFov = playerCamera.fieldOfView;
        }

        private void Start() {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update() {
            float deltaTime = Time.deltaTime;

            HandleCursorToggle();
            ApplyLook();
            UpdateTimers(deltaTime);
            ApplyHeadBob(deltaTime);
            ApplyCrouch(deltaTime);
            ApplyVerticalMovement(deltaTime);
            ApplyHorizontalMovement(deltaTime);
            ApplyFov(deltaTime);
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
            bool wasGrounded = isGrounded;
            isGrounded = characterController.isGrounded;

            if (!wasGrounded && isGrounded) {
                headBobVelocity -= Mathf.Abs(verticalVelocity) * headBobKickScale;
            }

            lastJumpTime += deltaTime;
            timeSinceJumpTriggered += deltaTime;

            if (isGrounded || isJumping) {
                lastJumpApexTime = 99f;
            } else {
                lastJumpApexTime += deltaTime;
            }

            if (isGrounded) {
                timeSinceGrounded = 0f;
            } else {
                timeSinceGrounded += deltaTime;
            }

            if (jumpPressedThisFrame) {
                lastJumpPressedTime = 0f;
            } else {
                lastJumpPressedTime += deltaTime;
            }
        }

        private void ApplyHeadBob(float deltaTime) {
            headBobVelocity += (-headBobOffset * headBobSpringStiffness - headBobVelocity * headBobSpringDamping) * deltaTime;
            headBobOffset = Mathf.Clamp(headBobOffset + headBobVelocity * deltaTime, -headBobMaxOffset, headBobMaxOffset);
        }

        private void ApplyCrouch(float deltaTime) {
            float progressionRate = isCrouchPressed
                ? (isGrounded ? crouchDownSpeed : airCrouchDownSpeed)
                : -(isGrounded ? crouchUpSpeed : airCrouchUpSpeed);

            crouchProgression = Mathf.Clamp01(crouchProgression + progressionRate * deltaTime);

            float height = Mathf.Lerp(standingHeight, crouchingHeight, crouchProgression);
            characterController.height = height;
            characterController.center = new Vector3(0f, height / 2f, 0f);

            Vector3 crouchedCameraPosition = Vector3.Lerp(standingCameraLocalPosition, crouchingCameraLocalPosition, crouchProgression);
            cameraRoot.localPosition = crouchedCameraPosition + new Vector3(0f, headBobOffset, 0f);
        }

        private void ApplyVerticalMovement(float deltaTime) {
            if (!isGrounded) {
                verticalVelocity += (timeSinceGrounded <= jumpCoyoteTime ? coyoteGravity : gravity) * deltaTime;
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
                    || (jumpPressedThisFrame && timeSinceGrounded <= jumpCoyoteTime)
                    || (lastJumpPressedTime <= jumpBufferTime && isGrounded)
                );

                if (canJumpNow) {
                    verticalVelocity = Mathf.Lerp(jumpVelocity, crouchJumpVelocity, crouchProgression);
                    currentJumpVelocitySustain = Mathf.Lerp(jumpVelocitySustain, crouchJumpVelocitySustain, crouchProgression);

                    lastJumpPressedTime = 99f;
                    timeSinceGrounded = 99f;
                    lastJumpTime = 0f;
                    timeSinceJumpTriggered = 0f;
                    isJumping = true;
                    isJumpSustainReleased = jumpReleasedThisFrame || !isJumpHeld;
                }
            }

            verticalVelocity = Mathf.Clamp(verticalVelocity, -maxFallSpeed, maxFallSpeed);
        }

        private void ApplyHorizontalMovement(float deltaTime) {
            bool hasMoveInput = moveInput.sqrMagnitude > 0.0001f;
            Vector3 inputDirection = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;

            if (isSliding) {
                if (!isCrouchPressed || !isGrounded) {
                    isSliding = false;
                }
            } else if (isGrounded && isCrouchPressed && horizontalVelocity.magnitude >= slideMinSpeed) {
                StartSlide();
            }

            if (isSliding) {
                ApplySlideMovement(deltaTime);
            } else if (isGrounded) {
                ApplyGroundMovement(deltaTime, inputDirection, hasMoveInput);
            } else {
                ApplyAirMovement(deltaTime, inputDirection, hasMoveInput);
            }
        }

        private void StartSlide() {
            isSliding = true;
            Vector3 slideDirection = horizontalVelocity.sqrMagnitude > 0.0001f ? horizontalVelocity.normalized : transform.forward;
            float boostedSpeed = Mathf.Min(horizontalVelocity.magnitude + slideBoostAmount, slideMaxSpeed);
            horizontalVelocity = slideDirection * boostedSpeed;
        }

        private void ApplySlideMovement(float deltaTime) {
            float speed = Mathf.Max(0f, horizontalVelocity.magnitude - slideFriction * deltaTime);
            horizontalVelocity = speed > 0.0001f ? horizontalVelocity.normalized * speed : Vector3.zero;

            if (speed < slideMinSpeed) {
                isSliding = false;
            }
        }

        private void ApplyGroundMovement(float deltaTime, Vector3 inputDirection, bool hasMoveInput) {
            float baseSpeed = isSprinting ? sprintSpeed : groundSpeed;
            float targetSpeed = Mathf.Lerp(baseSpeed, crouchSpeed, crouchProgression);

            float targetAcceleration = hasMoveInput
                ? Mathf.Lerp(groundAcceleration, crouchAcceleration, crouchProgression)
                : Mathf.Lerp(groundDeceleration, crouchDeceleration, crouchProgression);

            Vector3 targetVelocity = inputDirection * targetSpeed;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, targetAcceleration * deltaTime);
        }

        private void ApplyAirMovement(float deltaTime, Vector3 inputDirection, bool hasMoveInput) {
            if (!hasMoveInput) {
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, airDeceleration * deltaTime);
                return;
            }

            float wishSpeed = isSprinting ? sprintSpeed : groundSpeed;
            float accel = timeSinceJumpTriggered <= jumpControlWindowDuration ? jumpControlAcceleration : airAcceleration;

            float currentSpeedAlongWish = Vector3.Dot(horizontalVelocity, inputDirection);
            float addSpeed = wishSpeed - currentSpeedAlongWish;

            if (addSpeed <= 0f) {
                return;
            }

            float accelSpeed = Mathf.Min(accel * wishSpeed * deltaTime, addSpeed);
            horizontalVelocity += inputDirection * accelSpeed;
        }

        private void ApplyFov(float deltaTime) {
            float speedFraction = Mathf.Clamp01(horizontalVelocity.magnitude / sprintSpeed);
            float targetFov = baseFov + speedFraction * fovBoostAmount;
            playerCamera.fieldOfView = Mathf.MoveTowards(playerCamera.fieldOfView, targetFov, fovChangeSpeed * deltaTime);
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
            moveInput = value.Get<Vector2>();
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
