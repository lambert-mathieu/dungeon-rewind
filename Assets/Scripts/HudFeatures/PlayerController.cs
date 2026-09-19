using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Health")]
    public int health = 200;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -15f;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private Transform cameraTransform;

    [Header("Rewind Stamina Settings")]
    [SerializeField] private KeyCode staminaKey = KeyCode.Q; // Changed from Spacebar
    [SerializeField] private int playerLevel = 1;
    [SerializeField] private float baseStaminaSeconds = 2f;
    [SerializeField] private float secondsPerLevel = 1f;
    [SerializeField] private float staminaRechargeRate = 1.5f;
    [SerializeField] private float reloadPenaltyCooldown = 2f;

    private CharacterController controller;
    private float currentStamina;
    private float maxStamina;
    private float penaltyTimer;
    private bool isExhausted;

    private float verticalVelocity;
    private float cameraPitch = 0f;

    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    public bool IsExhausted => isExhausted;
    public bool CanRewind => !isExhausted && currentStamina > 0f;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        CalculateMaxStamina();
        currentStamina = maxStamina;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
        HandleStamina();
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -85f, 85f);

        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 move = transform.right * horizontal + transform.forward * vertical;

        // Ground check & Jump logic
        if (controller.isGrounded)
        {
            // Reset velocity downward to keep player snapped to slopes/floor
            if (verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            // Jump when Space is pressed
            if (Input.GetKeyDown(KeyCode.Space))
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
        else
        {
            // Apply gravity while in the air
            verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 velocity = move * moveSpeed + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleStamina()
    {
        // Check input using the assigned key (Left Shift by default)
        bool isRewinding = Input.GetKey(staminaKey);

        if (isRewinding && CanRewind)
        {
            ConsumeStamina(Time.deltaTime);
        }
        else
        {
            RechargeStamina(Time.deltaTime);
        }
    }

    public void ConsumeStamina(float amount)
    {
        currentStamina -= amount;

        if (currentStamina <= 0f)
        {
            currentStamina = 0f;
            isExhausted = true;
            penaltyTimer = reloadPenaltyCooldown;
        }
    }

    private void RechargeStamina(float deltaTime)
    {
        if (penaltyTimer > 0f)
        {
            penaltyTimer -= deltaTime;
            return;
        }

        if (currentStamina < maxStamina)
        {
            currentStamina += staminaRechargeRate * deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);

            if (isExhausted && currentStamina >= maxStamina * 0.25f)
            {
                isExhausted = false;
            }
        }
    }

    public void SetPlayerLevel(int newLevel)
    {
        playerLevel = newLevel;
        CalculateMaxStamina();
    }

    private void CalculateMaxStamina()
    {
        maxStamina = baseStaminaSeconds + (playerLevel * secondsPerLevel);
    }
}