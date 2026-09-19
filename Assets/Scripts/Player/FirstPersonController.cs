using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonRewind.Player {
    [RequireComponent(typeof(Rigidbody))]
    public class FirstPersonController : MonoBehaviour {
        private const float groundSpeed = 10.0f;
        private const float crouchSpeed = 7.0f;
        private const float groundAcceleration = 180.0f;
        private const float groundDeceleration = 90.0f;
        private const float crouchAcceleration = 90.0f;
        private const float crouchDeceleration = 45.0f;
        private const float airAcceleration = 50.0f;
        private const float airDeceleration = 25.0f;
        private const float speedTransitionSpeed = 10.0f;
        private const float speedTransitionTime = 1.0f;

        private const float crouchHeightScale = 0.75f;
        private const float crouchDownSpeed = 5.8f;
        private const float crouchUpSpeed = 4.8f;
        private const float airCrouchDownSpeed = 7.5f;
        private const float airCrouchUpSpeed = 12.0f;

        private const float slideHeightScale = 0.58f;
        private const float slideDuration = 0.60f;
        private const float slideBufferTime = 0.3f;
        private const float slideMinZeroTime = 0.3f;
        private const float slideDownSpeed = 7.5f;
        private const float slideUpSpeed = 6.5f;
        private const float airSlideDownSpeed = 12.0f;
        private const float airSlideUpSpeed = 14.0f;
        private const float slideSpeed = 12.0f;
        private const float slideAcceleration = 90.0f;
        private const float slideDeceleration = 45.0f;

        private const float gravity = -16.0f;
        private const float coyoteGravity = -10.0f;
        private const float maxFallSpeed = 35.0f;
        private const float jumpVelocity = 6.3f;
        private const float jumpVelocitySustain = 3.6f;
        private const float crouchJumpVelocity = 5.8f;
        private const float crouchJumpVelocitySustain = 3.6f;
        private const float slideJumpVelocity = 6.3f;
        private const float slideJumpVelocitySustain = 3.6f;
        private const float jumpCooldownTime = 0.02f;
        private const float jumpCoyoteTime = 0.1f;
        private const float jumpBufferTime = 0.15f;
        private const float jumpMinHoldTime = 0.0f;
        private const float jumpMaxHoldTime = 0.2f;
        private const float jumpApexFallBonusGravity = -13.0f;
        private const float jumpApexFallBonusTime = 0.25f;

        private const float mouseSensitivity = 0.12f;
        private const float minPitch = -88.0f;
        private const float maxPitch = 88.0f;

        private const float groundCheckRadius = 0.49f;
        private const float groundCheckSkin = 0.03f;
        private const int groundCheckRayCount = 20;
        private const float slopeLimit = 45.0f;

        [SerializeField] private Transform cameraRoot;
        [SerializeField] private Transform playerScale;
        [SerializeField] private Transform groundCheck;

        private Rigidbody rb;
        private LayerMask groundCheckMask;
        private Vector2 moveInput;
        private Vector2 lookInput;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private float pitch;

        private bool isCrouchPressed;
        private bool isJumpHeld;
        private bool jumpPressedThisFrame;
        private bool jumpReleasedThisFrame;
        private bool crouchPressedThisFrame;
        private bool crouchInputSuppressed;
        private bool effectiveCrouchPressed;

        private bool isGrounded;
        private float crouchProgression;
        private float currentSpeed;
        private float timeSinceGrounded = 99f;
        private float coyoteTimer = 99f;
        private float lastJumpPressedTime = 99f;
        private float lastJumpTime = 99f;
        private float lastJumpApexTime = 99f;
        private float lastCrouchPressedTime = 99f;
        private bool isSliding;
        private float slideProgression;
        private float slideTimer;
        private float timeSinceSlideZero = 99f;
        private bool slideDurationEndedThisFrame;
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
            ApplySlide(deltaTime, inputDirection, hasMoveInput);
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
            groundNormal = Vector3.up;
            bool grounded = false;
            float bestAngle = slopeLimit;

            for (int i = 0; i <= groundCheckRayCount; i++) {
                Vector3 offset = Vector3.zero;
                if (i < groundCheckRayCount) {
                    float angle = i * Mathf.PI * 2f / groundCheckRayCount;
                    offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * groundCheckRadius;
                }

                Vector3 origin = groundCheck.position + offset + Vector3.up * groundCheckSkin;

                if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundCheckSkin * 2f, groundCheckMask, QueryTriggerInteraction.Ignore)) {
                    continue;
                }

                float angleFromUp = Vector3.Angle(hit.normal, Vector3.up);
                if (angleFromUp > bestAngle) {
                    continue;
                }

                grounded = true;
                bestAngle = angleFromUp;
                groundNormal = hit.normal;
            }

            return grounded;
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

            if (crouchPressedThisFrame) {
                lastCrouchPressedTime = 0f;
            } else {
                lastCrouchPressedTime += deltaTime;
            }

            effectiveCrouchPressed = isCrouchPressed && !crouchInputSuppressed;
        }

        private void ApplySlide(float deltaTime, Vector3 inputDirection, bool hasMoveInput) {
            bool isForwardPressed = moveInput.y > 0f;

            if (isSliding) {
                slideTimer += deltaTime;

                if (slideTimer >= slideDuration) {
                    isSliding = false;
                    slideDurationEndedThisFrame = true;
                } else if (!effectiveCrouchPressed || !isForwardPressed) {
                    isSliding = false;
                }
            } else {
                bool crouchTriggerBuffered = isGrounded && lastCrouchPressedTime <= slideBufferTime;
                bool velocityAlignedWithInput = hasMoveInput && Vector3.Dot(inputDirection, horizontalVelocity) > 0f;
                bool canStartSlide = crouchTriggerBuffered && velocityAlignedWithInput && isForwardPressed && timeSinceSlideZero >= slideMinZeroTime;

                if (canStartSlide) {
                    isSliding = true;
                    slideTimer = 0f;
                }
            }

            float progressionRate = isSliding ? (isGrounded ? slideDownSpeed : airSlideDownSpeed) : -(isGrounded ? slideUpSpeed : airSlideUpSpeed);
            slideProgression = Mathf.Clamp01(slideProgression + progressionRate * deltaTime);

            timeSinceSlideZero = slideProgression <= 0f ? timeSinceSlideZero + deltaTime : 0f;
        }

        private void ApplyCrouch(float deltaTime) {
            if (isSliding) {
                crouchProgression = 0f;
            } else if (slideDurationEndedThisFrame) {
                crouchProgression = 1f;
            } else {
                float progressionRate = effectiveCrouchPressed ? (isGrounded ? crouchDownSpeed : airCrouchDownSpeed) : -(isGrounded ? crouchUpSpeed : airCrouchUpSpeed);
                crouchProgression = Mathf.Clamp01(crouchProgression + progressionRate * deltaTime);
            }

            float crouchHeight = Mathf.Lerp(1f, crouchHeightScale, crouchProgression);
            float heightScale = Mathf.Lerp(crouchHeight, slideHeightScale, slideProgression);
            float previousGroundCheckHeight = groundCheck.position.y;

            Vector3 scale = playerScale.localScale;
            scale.y = heightScale;
            playerScale.localScale = scale;

            if (isGrounded) {
                rb.position += Vector3.up * (previousGroundCheckHeight - groundCheck.position.y);
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
                    if (isSliding) {
                        isSliding = false;
                        crouchInputSuppressed = true;
                    }

                    float crouchedJumpVelocity = Mathf.Lerp(jumpVelocity, crouchJumpVelocity, crouchProgression);
                    float crouchedJumpVelocitySustain = Mathf.Lerp(jumpVelocitySustain, crouchJumpVelocitySustain, crouchProgression);

                    verticalVelocity = Mathf.Lerp(crouchedJumpVelocity, slideJumpVelocity, slideProgression);
                    currentJumpVelocitySustain = Mathf.Lerp(crouchedJumpVelocitySustain, slideJumpVelocitySustain, slideProgression);

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
                float crouchedSpeed = Mathf.Lerp(groundSpeed, crouchSpeed, crouchProgression);
                currentSpeed = Mathf.Lerp(crouchedSpeed, slideSpeed, slideProgression);
            } else if (timeSinceGrounded >= speedTransitionTime && currentSpeed < groundSpeed) {
                currentSpeed = Mathf.Min(currentSpeed + speedTransitionSpeed * deltaTime, groundSpeed);
            }

            float baseAcceleration = hasMoveInput
                ? (isGrounded ? Mathf.Lerp(groundAcceleration, crouchAcceleration, crouchProgression) : airAcceleration)
                : (isGrounded ? Mathf.Lerp(groundDeceleration, crouchDeceleration, crouchProgression) : airDeceleration);

            float acceleration = Mathf.Lerp(baseAcceleration, hasMoveInput ? slideAcceleration : slideDeceleration, slideProgression);

            Vector3 targetDirection = inputDirection;
            if (hasMoveInput) {
                Vector3 cameraForward = cameraRoot.forward;
                cameraForward.y = 0f;
                if (cameraForward.sqrMagnitude > 0.0001f) {
                    targetDirection = Vector3.Slerp(inputDirection, cameraForward.normalized, 0.5f * slideProgression);
                }
            }

            Vector3 targetVelocity = targetDirection * currentSpeed;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, acceleration * deltaTime);
        }

        private void ApplyMotion() {
            rb.linearVelocity = new Vector3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.z);
        }

        private void ConsumeFrameInputFlags() {
            jumpPressedThisFrame = false;
            jumpReleasedThisFrame = false;
            crouchPressedThisFrame = false;
            slideDurationEndedThisFrame = false;
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

        public void OnCrouch(InputValue value) {
            bool wasCrouchPressed = isCrouchPressed;
            isCrouchPressed = value.isPressed;

            if (isCrouchPressed && !wasCrouchPressed) {
                crouchPressedThisFrame = true;
            } else if (!isCrouchPressed && wasCrouchPressed) {
                crouchInputSuppressed = false;
            }
        }
    }
}
