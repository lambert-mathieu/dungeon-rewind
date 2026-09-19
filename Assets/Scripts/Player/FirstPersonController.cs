using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonRewind.Player {
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour {
        private const float walkSpeed = 5f;
        private const float sprintSpeed = 8f;
        private const float crouchSpeed = 2.5f;
        private const float acceleration = 12f;
        private const float jumpHeight = 1.2f;
        private const float gravity = -18f;
        private const float groundedStickForce = -2f;

        private const float mouseSensitivity = 0.12f;
        private const float minPitch = -85f;
        private const float maxPitch = 85f;

        private const float standingHeight = 1.8f;
        private const float crouchingHeight = 1f;
        private const float crouchTransitionSpeed = 10f;

        [SerializeField] private Transform cameraRoot;

        private CharacterController characterController;
        private Vector2 moveInput;
        private Vector2 lookInput;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private float pitch;
        private bool isSprinting;
        private bool isCrouching;
        private bool jumpQueued;
        private Vector3 standingCameraLocalPosition;
        private Vector3 crouchingCameraLocalPosition;

        private void Awake() {
            characterController = GetComponent<CharacterController>();
            characterController.height = standingHeight;
            characterController.center = new Vector3(0f, standingHeight / 2f, 0f);

            standingCameraLocalPosition = cameraRoot.localPosition;
            float heightDifference = standingHeight - crouchingHeight;
            crouchingCameraLocalPosition = standingCameraLocalPosition - new Vector3(0f, heightDifference, 0f);
        }

        private void Start() {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update() {
            HandleCursorToggle();
            ApplyLook();
            ApplyCrouch();
            ApplyMovement();
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

        private void ApplyCrouch() {
            float targetHeight = isCrouching ? crouchingHeight : standingHeight;
            characterController.height = Mathf.Lerp(characterController.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
            characterController.center = new Vector3(0f, characterController.height / 2f, 0f);

            Vector3 targetCameraLocalPosition = isCrouching ? crouchingCameraLocalPosition : standingCameraLocalPosition;
            cameraRoot.localPosition = Vector3.Lerp(cameraRoot.localPosition, targetCameraLocalPosition, crouchTransitionSpeed * Time.deltaTime);
        }

        private void ApplyMovement() {
            float targetSpeed = isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);
            Vector3 inputDirection = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;
            Vector3 targetVelocity = inputDirection * targetSpeed;

            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, acceleration * Time.deltaTime);

            if (characterController.isGrounded) {
                verticalVelocity = groundedStickForce;

                if (jumpQueued) {
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }
            } else {
                verticalVelocity += gravity * Time.deltaTime;
            }

            jumpQueued = false;

            Vector3 motion = new Vector3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.z);
            characterController.Move(motion * Time.deltaTime);
        }

        public void OnMove(InputValue value) {
            moveInput = value.Get<Vector2>();
        }

        public void OnLook(InputValue value) {
            lookInput = value.Get<Vector2>();
        }

        public void OnJump(InputValue value) {
            if (value.isPressed) {
                jumpQueued = true;
            }
        }

        public void OnSprint(InputValue value) {
            isSprinting = value.isPressed;
        }

        public void OnCrouch(InputValue value) {
            isCrouching = value.isPressed;
        }
    }
}
