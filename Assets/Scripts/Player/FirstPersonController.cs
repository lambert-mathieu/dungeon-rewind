using DungeonRewind.Rewind;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonRewind.Player {
    [RequireComponent(typeof(Rigidbody))]
    public class FirstPersonController : RewindRecorder<PlayerControllerState>, IRewindSuspendable {
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
        private const float groundedStickVerticalVelocity = -2.0f;
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

        private const float groundCheckRadius = 0.49f;
        private const float groundCheckSkin = 0.03f;
        private const int groundCheckRayCount = 20;
        private const float slopeLimit = 45.0f;

        [SerializeField] private Transform cameraRoot;
        [SerializeField] private Transform playerScale;
        [SerializeField] private Transform groundCheck;

        private Rigidbody rb;
        private PlayerControllerState state;
        private bool isSuspended;
        private Vector3 groundNormal = Vector3.up;

        private Vector2 moveInput;
        private bool isCrouchPressed;
        private bool isJumpHeld;
        private bool jumpPressedThisFrame;
        private bool jumpReleasedThisFrame;
        private bool crouchPressedThisFrame;
        private bool crouchInputSuppressed;

        protected override void Initialize() {
            rb = GetComponent<Rigidbody>();
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.useGravity = false;

            playerScale.localScale = Vector3.one;

            state = PlayerControllerState.CreateInitial(~(1 << gameObject.layer), groundSpeed);
        }

        protected override PlayerControllerState Capture() => state;

        protected override void Apply(PlayerControllerState snapshot) {
            state = snapshot;
        }

        public void SuspendForRewind() {
            isSuspended = true;
        }

        public void ResumeAfterRewind() {
            isSuspended = false;
            ConsumeFrameInputFlags();
        }

        private void FixedUpdate() {
            if (isSuspended) {
                return;
            }

            float deltaTime = Time.fixedDeltaTime;
            bool hasMoveInput = moveInput.sqrMagnitude > 0.0001f;
            Vector3 inputDirection = (cameraRoot.right * moveInput.x + cameraRoot.forward * moveInput.y).normalized;
            bool wasGroundedLastStep = state.IsGrounded;

            UpdateTimers(deltaTime);
            ApplySlide(deltaTime, inputDirection, hasMoveInput);
            ApplyCrouch(deltaTime);
            ApplyVerticalMovement(deltaTime, wasGroundedLastStep);
            ApplyHorizontalMovement(deltaTime, inputDirection, hasMoveInput);
            ApplyMotion(wasGroundedLastStep);
            state.SlideDurationEndedThisFrame = false;
            ConsumeFrameInputFlags();
        }

        private bool GroundCheck(out Vector3 hitGroundNormal) {
            hitGroundNormal = Vector3.up;
            bool grounded = false;
            float bestAngle = slopeLimit;

            for (int i = 0; i <= groundCheckRayCount; i++) {
                Vector3 offset = Vector3.zero;
                if (i < groundCheckRayCount) {
                    float angle = i * Mathf.PI * 2f / groundCheckRayCount;
                    offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * groundCheckRadius;
                }

                Vector3 origin = groundCheck.position + offset + Vector3.up * groundCheckSkin;

                if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundCheckSkin * 2f, state.GroundCheckMask, QueryTriggerInteraction.Ignore)) {
                    continue;
                }

                float angleFromUp = Vector3.Angle(hit.normal, Vector3.up);
                if (angleFromUp > bestAngle) {
                    continue;
                }

                grounded = true;
                bestAngle = angleFromUp;
                hitGroundNormal = hit.normal;
            }

            return grounded;
        }

        private void UpdateTimers(float deltaTime) {
            state.IsGrounded = GroundCheck(out groundNormal);

            state.LastJumpTime += deltaTime;

            if (state.IsGrounded || state.IsJumping) {
                state.LastJumpApexTime = 99f;
            } else {
                state.LastJumpApexTime += deltaTime;
            }

            if (state.IsGrounded) {
                state.TimeSinceGrounded = 0f;
                state.CoyoteTimer = 0f;
            } else {
                state.TimeSinceGrounded += deltaTime;
                state.CoyoteTimer += deltaTime;
            }

            if (jumpPressedThisFrame) {
                state.LastJumpPressedTime = 0f;
            } else {
                state.LastJumpPressedTime += deltaTime;
            }

            if (crouchPressedThisFrame) {
                state.LastCrouchPressedTime = 0f;
            } else {
                state.LastCrouchPressedTime += deltaTime;
            }

            state.EffectiveCrouchPressed = isCrouchPressed && !crouchInputSuppressed;
        }

        private void ApplySlide(float deltaTime, Vector3 inputDirection, bool hasMoveInput) {
            bool isForwardPressed = moveInput.y > 0f;

            if (state.IsSliding) {
                state.SlideTimer += deltaTime;

                if (state.SlideTimer >= slideDuration) {
                    state.IsSliding = false;
                    state.SlideDurationEndedThisFrame = true;
                } else if (!state.EffectiveCrouchPressed || !isForwardPressed) {
                    state.IsSliding = false;
                }
            } else {
                bool crouchTriggerBuffered = state.IsGrounded && state.LastCrouchPressedTime <= slideBufferTime;
                bool velocityAlignedWithInput = hasMoveInput && Vector3.Dot(inputDirection, state.HorizontalVelocity) > 0f;
                bool canStartSlide = crouchTriggerBuffered && velocityAlignedWithInput && isForwardPressed && state.TimeSinceSlideZero >= slideMinZeroTime;

                if (canStartSlide) {
                    state.IsSliding = true;
                    state.SlideTimer = 0f;
                }
            }

            float progressionRate = state.IsSliding ? (state.IsGrounded ? slideDownSpeed : airSlideDownSpeed) : -(state.IsGrounded ? slideUpSpeed : airSlideUpSpeed);
            state.SlideProgression = Mathf.Clamp01(state.SlideProgression + progressionRate * deltaTime);

            state.TimeSinceSlideZero = state.SlideProgression <= 0f ? state.TimeSinceSlideZero + deltaTime : 0f;
        }

        private void ApplyCrouch(float deltaTime) {
            if (state.IsSliding) {
                state.CrouchProgression = 0f;
            } else if (state.SlideDurationEndedThisFrame) {
                state.CrouchProgression = 1f;
            } else {
                float progressionRate = state.EffectiveCrouchPressed ? (state.IsGrounded ? crouchDownSpeed : airCrouchDownSpeed) : -(state.IsGrounded ? crouchUpSpeed : airCrouchUpSpeed);
                state.CrouchProgression = Mathf.Clamp01(state.CrouchProgression + progressionRate * deltaTime);
            }

            float crouchHeight = Mathf.Lerp(1f, crouchHeightScale, state.CrouchProgression);
            float heightScale = Mathf.Lerp(crouchHeight, slideHeightScale, state.SlideProgression);
            float previousGroundCheckHeight = groundCheck.position.y;

            Vector3 scale = playerScale.localScale;
            scale.y = heightScale;
            playerScale.localScale = scale;

            if (state.IsGrounded) {
                rb.position += Vector3.up * (previousGroundCheckHeight - groundCheck.position.y);
            }
        }

        private void ApplyVerticalMovement(float deltaTime, bool wasGroundedLastStep) {
            if (!state.IsGrounded) {
                state.VerticalVelocity += (state.CoyoteTimer <= jumpCoyoteTime ? coyoteGravity : gravity) * deltaTime;
            } else if (wasGroundedLastStep) {
                state.VerticalVelocity = groundedStickVerticalVelocity;
            }

            if (state.LastJumpApexTime <= jumpApexFallBonusTime) {
                state.VerticalVelocity += jumpApexFallBonusGravity * deltaTime;
            }

            if (state.IsJumping) {
                if (jumpReleasedThisFrame || !isJumpHeld) {
                    state.IsJumpSustainReleased = true;
                }

                bool shouldEndSustain = (state.IsJumpSustainReleased && state.LastJumpTime >= jumpMinHoldTime)
                    || state.LastJumpTime >= jumpMaxHoldTime
                    || state.VerticalVelocity <= 1f
                    || state.IsGrounded;

                if (shouldEndSustain) {
                    state.IsJumping = false;
                    state.LastJumpApexTime = 0f;
                } else {
                    state.VerticalVelocity += state.CurrentJumpVelocitySustain * deltaTime;
                }
            } else {
                bool canJumpNow = state.LastJumpTime >= jumpCooldownTime && (
                    (jumpPressedThisFrame && state.IsGrounded)
                    || (jumpPressedThisFrame && state.CoyoteTimer <= jumpCoyoteTime)
                    || (state.LastJumpPressedTime <= jumpBufferTime && state.IsGrounded)
                );

                if (canJumpNow) {
                    if (state.IsSliding) {
                        state.IsSliding = false;
                        crouchInputSuppressed = true;
                    }

                    float crouchedJumpVelocity = Mathf.Lerp(jumpVelocity, crouchJumpVelocity, state.CrouchProgression);
                    float crouchedJumpVelocitySustain = Mathf.Lerp(jumpVelocitySustain, crouchJumpVelocitySustain, state.CrouchProgression);

                    state.VerticalVelocity = Mathf.Lerp(crouchedJumpVelocity, slideJumpVelocity, state.SlideProgression);
                    state.CurrentJumpVelocitySustain = Mathf.Lerp(crouchedJumpVelocitySustain, slideJumpVelocitySustain, state.SlideProgression);

                    state.LastJumpPressedTime = 99f;
                    state.CoyoteTimer = 99f;
                    state.LastJumpTime = 0f;
                    state.IsJumping = true;
                    state.IsJumpSustainReleased = jumpReleasedThisFrame || !isJumpHeld;
                }
            }

            state.VerticalVelocity = Mathf.Clamp(state.VerticalVelocity, -maxFallSpeed, maxFallSpeed);
        }

        private void ApplyHorizontalMovement(float deltaTime, Vector3 inputDirection, bool hasMoveInput) {
            if (state.IsGrounded) {
                float crouchedSpeed = Mathf.Lerp(groundSpeed, crouchSpeed, state.CrouchProgression);
                state.CurrentSpeed = Mathf.Lerp(crouchedSpeed, slideSpeed, state.SlideProgression);
            } else if (state.TimeSinceGrounded >= speedTransitionTime && state.CurrentSpeed < groundSpeed) {
                state.CurrentSpeed = Mathf.Min(state.CurrentSpeed + speedTransitionSpeed * deltaTime, groundSpeed);
            }

            float baseAcceleration = hasMoveInput
                ? (state.IsGrounded ? Mathf.Lerp(groundAcceleration, crouchAcceleration, state.CrouchProgression) : airAcceleration)
                : (state.IsGrounded ? Mathf.Lerp(groundDeceleration, crouchDeceleration, state.CrouchProgression) : airDeceleration);

            float acceleration = Mathf.Lerp(baseAcceleration, hasMoveInput ? slideAcceleration : slideDeceleration, state.SlideProgression);

            Vector3 targetDirection = inputDirection;
            if (hasMoveInput) {
                Vector3 cameraForward = cameraRoot.forward;
                cameraForward.y = 0f;
                if (cameraForward.sqrMagnitude > 0.0001f) {
                    targetDirection = Vector3.Slerp(inputDirection, cameraForward.normalized, 0.5f * state.SlideProgression);
                }
            }

            Vector3 targetVelocity = targetDirection * state.CurrentSpeed;
            state.HorizontalVelocity = Vector3.MoveTowards(state.HorizontalVelocity, targetVelocity, acceleration * deltaTime);
        }

        private void ApplyMotion(bool wasGroundedLastStep) {
            Vector3 horizontalVelocity = new Vector3(state.HorizontalVelocity.x, 0f, state.HorizontalVelocity.z);

            if (state.IsGrounded && wasGroundedLastStep && !state.IsJumping) {
                rb.linearVelocity = horizontalVelocity + groundNormal * state.VerticalVelocity;
            } else {
                rb.linearVelocity = horizontalVelocity + Vector3.up * state.VerticalVelocity;
            }
        }

        private void ConsumeFrameInputFlags() {
            jumpPressedThisFrame = false;
            jumpReleasedThisFrame = false;
            crouchPressedThisFrame = false;
        }

        public Vector3 HorizontalVelocity => state.HorizontalVelocity;
        public float VerticalVelocity => state.VerticalVelocity;
        public bool IsGrounded => state.IsGrounded;
        public bool IsSuspended => isSuspended;

        public void OnMove(InputValue value) {
            moveInput = Vector2.ClampMagnitude(value.Get<Vector2>(), 1f);
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
