using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonRewind.Player {
    [RequireComponent(typeof(Camera))]
    public class PlayerCamera : MonoBehaviour {
        private const float mouseSensitivity = 0.12f;
        private const float minPitch = -88.0f;
        private const float maxPitch = 88.0f;

        [SerializeField] private Camera armCamera;
        [SerializeField] private FirstPersonController firstPersonController;
        [SerializeField] private Transform armRight;
        private const float referenceLowSpeed = 8.0f;
        private const float referenceHighSpeed = 20.0f;
        private const float directionFullBoostAngle = 40.0f;
        private const float directionZeroBoostAngle = 100.0f;
        private const float maxFieldOfViewBoost = 20.0f;
        private const float fieldOfViewIncreaseSpeed = 10.0f;
        private const float fieldOfViewDecreaseSpeed = 30.0f;
        private const float minHandSwayAmplitude = 0.01f;
        private const float maxHandSwayAmplitude = 0.05f;
        private const float minHandSwayFrequency = 1.0f;
        private const float maxHandSwayFrequency = 4.0f;

        private Vector2 lookInput;
        private float pitch;
        private float baseFieldOfView;
        private Vector3 armRightBaseLocalPosition;
        private float handSwayPhase;

        private void Awake() {
            baseFieldOfView = armCamera.fieldOfView;
            armRightBaseLocalPosition = armRight.localPosition;
        }

        private void Start() {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update() {
            HandleCursorToggle();
            ApplyLook();

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f) {
                return;
            }

            float speedIntensity = CalculateSpeedIntensity();
            UpdateDynamicFieldOfView(speedIntensity, deltaTime);
            UpdateHandAnimation(speedIntensity, deltaTime);
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

            pitch = Mathf.Clamp(pitch - lookInput.y * mouseSensitivity, minPitch, maxPitch);
            transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private float CalculateSpeedIntensity() {
            Vector3 horizontalVelocity = firstPersonController.HorizontalVelocity;

            Vector3 cameraForward = transform.forward;
            cameraForward.y = 0f;
            float directionFactor = 0f;
            if (horizontalVelocity.sqrMagnitude > 0.0001f && cameraForward.sqrMagnitude > 0.0001f) {
                float velocityAngle = Vector3.Angle(horizontalVelocity.normalized, cameraForward.normalized);
                directionFactor = 1f - Mathf.InverseLerp(directionFullBoostAngle, directionZeroBoostAngle, velocityAngle);
            }

            float speedRatio = Mathf.InverseLerp(referenceLowSpeed, referenceHighSpeed, horizontalVelocity.magnitude);
            return speedRatio * directionFactor;
        }

        private void UpdateDynamicFieldOfView(float speedIntensity, float deltaTime) {
            float targetFieldOfView = baseFieldOfView + maxFieldOfViewBoost * speedIntensity;
            float transitionSpeed = targetFieldOfView > armCamera.fieldOfView ? fieldOfViewIncreaseSpeed : fieldOfViewDecreaseSpeed;
            armCamera.fieldOfView = Mathf.MoveTowards(armCamera.fieldOfView, targetFieldOfView, transitionSpeed * deltaTime);
        }

        private void UpdateHandAnimation(float speedIntensity, float deltaTime) {
            float amplitude = Mathf.Lerp(minHandSwayAmplitude, maxHandSwayAmplitude, speedIntensity);
            float frequency = Mathf.Lerp(minHandSwayFrequency, maxHandSwayFrequency, speedIntensity);

            handSwayPhase = Mathf.Repeat(handSwayPhase + frequency * deltaTime * Mathf.PI * 2f, Mathf.PI * 2f);

            float horizontalOffset = Mathf.Sin(handSwayPhase) * amplitude;
            float verticalOffset = Mathf.Sin(handSwayPhase * 2f) * amplitude * 0.5f;

            armRight.localPosition = armRightBaseLocalPosition + new Vector3(horizontalOffset, verticalOffset, 0f);
        }

        public void OnLook(InputValue value) {
            lookInput = value.Get<Vector2>();
        }
    }
}
