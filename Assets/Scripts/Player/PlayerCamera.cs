using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonRewind.Player {
    [RequireComponent(typeof(FirstPersonController))]
    public class PlayerCamera : MonoBehaviour {
        private const float mouseSensitivity = 0.12f;
        private const float minPitch = -88.0f;
        private const float maxPitch = 88.0f;

        [SerializeField] private Camera armCamera;
        [SerializeField] private Transform cameraRoot;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Transform arms;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform rightArm;
        private const float referenceLowSpeed = 8.0f;
        private const float referenceHighSpeed = 20.0f;
        private const float directionFullBoostAngle = 40.0f;
        private const float directionZeroBoostAngle = 100.0f;
        private const float maxFieldOfViewBoost = 20.0f;
        private const float fieldOfViewIncreaseSpeed = 5.0f;
        private const float fieldOfViewDecreaseSpeed = 10.0f;

        private const float handSwayReferenceLowSpeed = 0.0f;
        private const float handSwayReferenceMidSpeed = 10.0f;
        private const float handSwayReferenceHighSpeed = 12.0f;
        private const float minHandSwayAmplitude = 0.05f;
        private const float midHandSwayAmplitude = 0.10f;
        private const float maxHandSwayAmplitude = 0.08f;
        private const float minHandSwayFrequency = 0.3f;
        private const float midHandSwayFrequency = 1.2f;
        private const float maxHandSwayFrequency = 0.8f;
        private const float swayMagnitudeIncreaseSpeed = 2.0f;
        private const float swayMagnitudeDecreaseSpeed = 2.0f;
        private const float swayFrequencyIncreaseSpeed = 2.0f;
        private const float swayFrequencyDecreaseSpeed = 2.0f;
        private const float airborneHandSwayMultiplier = 0.6f;
        private const float airborneHandSpreadOffset = 0.10f;
        private const float airborneHandSpreadIncreaseSpeed = 0.5f;
        private const float airborneHandSpreadDecreaseSpeed = 2.0f;

        private const float verticalSwayStiffness = 81.0f;
        private const float verticalSwayDamping = 18.0f;
        private const float jumpSwayKickVelocity = 2.45f;
        private const float jumpDetectionVerticalVelocityThreshold = 3.0f;
        private const float minLandingSwayKickVelocity = 0.73f;
        private const float maxLandingSwayKickVelocity = 3.67f;
        private const float landingSwayReferenceSpeed = 12.0f;
        private const float maxVerticalSwayOffset = 0.25f;

        private FirstPersonController firstPersonController;

        private Vector2 lookInput;
        private float pitch;
        private float baseFieldOfView;
        private Vector3 armsBaseLocalPosition;
        private Vector3 leftArmBaseLocalPosition;
        private Vector3 rightArmBaseLocalPosition;
        private float handSwayPhase;
        private float currentHandSwayAmplitude;
        private float currentHandSwayFrequency;
        private float currentAirborneHandSpread;
        private bool hasGroundStateBaseline;
        private bool wasGroundedLastFrame;
        private float verticalSwayOffset;
        private float verticalSwaySpringVelocity;

        private void Awake() {
            firstPersonController = GetComponent<FirstPersonController>();
            baseFieldOfView = armCamera.fieldOfView;
            armsBaseLocalPosition = arms.localPosition;
            leftArmBaseLocalPosition = leftArm.localPosition;
            rightArmBaseLocalPosition = rightArm.localPosition;
        }

        private void Start() {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update() {
            HandleCursorToggle();
            ApplyLook();
            DetectJumpAndLandingEvents();

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f) {
                return;
            }

            Vector3 horizontalVelocity = firstPersonController.HorizontalVelocity;
            float directionFactor = CalculateDirectionFactor(horizontalVelocity);

            UpdateDynamicFieldOfView(CalculateFieldOfViewSpeedIntensity(horizontalVelocity, directionFactor), deltaTime);
            UpdateVerticalSway(deltaTime);
            UpdateHandAnimation(CalculateHandSwaySpeedIntensity(horizontalVelocity, directionFactor), deltaTime);
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

            if (!firstPersonController.IsSuspended) {
                cameraRoot.Rotate(Vector3.up * (lookInput.x * mouseSensitivity));
            }

            pitch = Mathf.Clamp(pitch - lookInput.y * mouseSensitivity, minPitch, maxPitch);
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private float CalculateDirectionFactor(Vector3 horizontalVelocity) {
            Vector3 cameraForward = cameraRoot.forward;
            if (horizontalVelocity.sqrMagnitude <= 0.0001f || cameraForward.sqrMagnitude <= 0.0001f) {
                return 0f;
            }

            float velocityAngle = Vector3.Angle(horizontalVelocity.normalized, cameraForward.normalized);
            return 1f - Mathf.InverseLerp(directionFullBoostAngle, directionZeroBoostAngle, velocityAngle);
        }

        private float CalculateFieldOfViewSpeedIntensity(Vector3 horizontalVelocity, float directionFactor) {
            float speedRatio = Mathf.InverseLerp(referenceLowSpeed, referenceHighSpeed, horizontalVelocity.magnitude);
            return speedRatio * directionFactor;
        }

        private static float InterpolateThreePoint(float low, float mid, float high, float t) {
            return t <= 0.5f
                ? Mathf.Lerp(low, mid, t / 0.5f)
                : Mathf.Lerp(mid, high, (t - 0.5f) / 0.5f);
        }

        private float CalculateHandSwaySpeedIntensity(Vector3 horizontalVelocity, float directionFactor) {
            float speed = horizontalVelocity.magnitude;
            float speedRatio = speed <= handSwayReferenceMidSpeed
                ? Mathf.InverseLerp(handSwayReferenceLowSpeed, handSwayReferenceMidSpeed, speed) * 0.5f
                : 0.5f + Mathf.InverseLerp(handSwayReferenceMidSpeed, handSwayReferenceHighSpeed, speed) * 0.5f;
            return speedRatio * directionFactor;
        }

        private void UpdateDynamicFieldOfView(float speedIntensity, float deltaTime) {
            float targetFieldOfView = baseFieldOfView + maxFieldOfViewBoost * speedIntensity;
            float transitionSpeed = targetFieldOfView > armCamera.fieldOfView ? fieldOfViewIncreaseSpeed : fieldOfViewDecreaseSpeed;
            armCamera.fieldOfView = Mathf.MoveTowards(armCamera.fieldOfView, targetFieldOfView, transitionSpeed * deltaTime);
        }

        private void UpdateHandAnimation(float speedIntensity, float deltaTime) {
            float targetAmplitude = InterpolateThreePoint(minHandSwayAmplitude, midHandSwayAmplitude, maxHandSwayAmplitude, speedIntensity);
            float targetFrequency = InterpolateThreePoint(minHandSwayFrequency, midHandSwayFrequency, maxHandSwayFrequency, speedIntensity);

            if (!firstPersonController.IsGrounded) {
                targetAmplitude *= airborneHandSwayMultiplier;
                targetFrequency *= airborneHandSwayMultiplier;
            }

            float targetAirborneHandSpread = firstPersonController.IsGrounded ? 0f : airborneHandSpreadOffset;
            float spreadTransitionSpeed = targetAirborneHandSpread > currentAirborneHandSpread ? airborneHandSpreadIncreaseSpeed : airborneHandSpreadDecreaseSpeed;
            currentAirborneHandSpread = Mathf.MoveTowards(currentAirborneHandSpread, targetAirborneHandSpread, spreadTransitionSpeed * deltaTime);

            float amplitudeTransitionSpeed = targetAmplitude > currentHandSwayAmplitude ? swayMagnitudeIncreaseSpeed : swayMagnitudeDecreaseSpeed;
            float frequencyTransitionSpeed = targetFrequency > currentHandSwayFrequency ? swayFrequencyIncreaseSpeed : swayFrequencyDecreaseSpeed;

            currentHandSwayAmplitude = Mathf.MoveTowards(currentHandSwayAmplitude, targetAmplitude, amplitudeTransitionSpeed * deltaTime);
            currentHandSwayFrequency = Mathf.MoveTowards(currentHandSwayFrequency, targetFrequency, frequencyTransitionSpeed * deltaTime);

            handSwayPhase = Mathf.Repeat(handSwayPhase + currentHandSwayFrequency * deltaTime * Mathf.PI * 2f, Mathf.PI * 2f);

            float horizontalOffset = Mathf.Sin(handSwayPhase) * currentHandSwayAmplitude;
            float verticalOffset = Mathf.Sin(handSwayPhase * 2f) * currentHandSwayAmplitude * 0.5f;

            arms.localPosition = armsBaseLocalPosition + new Vector3(0f, verticalSwayOffset, 0f);
            leftArm.localPosition = leftArmBaseLocalPosition + new Vector3(-horizontalOffset - currentAirborneHandSpread, -verticalOffset, 0f);
            rightArm.localPosition = rightArmBaseLocalPosition + new Vector3(horizontalOffset + currentAirborneHandSpread, verticalOffset, 0f);
        }

        private void DetectJumpAndLandingEvents() {
            bool isGroundedNow = firstPersonController.IsGrounded;

            if (!hasGroundStateBaseline) {
                hasGroundStateBaseline = true;
                wasGroundedLastFrame = isGroundedNow;
                return;
            }

            if (isGroundedNow && !wasGroundedLastFrame) {
                float impactSpeed = Mathf.Abs(firstPersonController.VerticalVelocity);
                float landingIntensity = Mathf.Clamp01(impactSpeed / landingSwayReferenceSpeed);
                ApplyVerticalSwayKick(-Mathf.Lerp(minLandingSwayKickVelocity, maxLandingSwayKickVelocity, landingIntensity));
            } else if (!isGroundedNow && wasGroundedLastFrame && firstPersonController.VerticalVelocity >= jumpDetectionVerticalVelocityThreshold) {
                ApplyVerticalSwayKick(-jumpSwayKickVelocity);
            }

            wasGroundedLastFrame = isGroundedNow;
        }

        private void ApplyVerticalSwayKick(float velocityKick) {
            verticalSwaySpringVelocity += velocityKick;
        }

        private void UpdateVerticalSway(float deltaTime) {
            verticalSwaySpringVelocity += (-verticalSwayStiffness * verticalSwayOffset - verticalSwayDamping * verticalSwaySpringVelocity) * deltaTime;
            verticalSwayOffset = Mathf.Clamp(verticalSwayOffset + verticalSwaySpringVelocity * deltaTime, -maxVerticalSwayOffset, maxVerticalSwayOffset);
        }

        public void OnLook(InputValue value) {
            lookInput = value.Get<Vector2>();
        }
    }
}
