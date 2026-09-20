using UnityEngine;

namespace DungeonRewind.Player {
    public struct PlayerControllerState {
        public LayerMask GroundCheckMask;
        public Vector3 HorizontalVelocity;
        public float VerticalVelocity;

        public bool EffectiveCrouchPressed;
        public bool IsGrounded;
        public float CrouchProgression;
        public float CurrentSpeed;
        public float TimeSinceGrounded;
        public float CoyoteTimer;
        public float LastJumpPressedTime;
        public float LastJumpTime;
        public float LastJumpApexTime;
        public float LastCrouchPressedTime;
        public bool IsSliding;
        public float SlideProgression;
        public float SlideTimer;
        public float TimeSinceSlideZero;
        public bool SlideDurationEndedThisFrame;
        public bool IsJumping;
        public bool IsJumpSustainReleased;
        public float CurrentJumpVelocitySustain;

        private const float expiredTimer = 99f;

        public static PlayerControllerState CreateInitial(LayerMask groundCheckMask, float initialSpeed) {
            return new PlayerControllerState {
                GroundCheckMask = groundCheckMask,
                CurrentSpeed = initialSpeed,
                TimeSinceGrounded = expiredTimer,
                CoyoteTimer = expiredTimer,
                LastJumpPressedTime = expiredTimer,
                LastJumpTime = expiredTimer,
                LastJumpApexTime = expiredTimer,
                LastCrouchPressedTime = expiredTimer,
                TimeSinceSlideZero = expiredTimer
            };
        }
    }
}
