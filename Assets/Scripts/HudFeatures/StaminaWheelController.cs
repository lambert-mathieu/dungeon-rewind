using UnityEngine;
using UnityEngine.UI;

public class StaminaWheelController : MonoBehaviour
{
    [Header("References")]
    public PlayerController player;
    [SerializeField] private Image fillImage;

    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = new Color(0.15f, 0.75f, 0.95f, 1f);
    [SerializeField] private Color exhaustedColor = new Color(0.9f, 0.2f, 0.2f, 1f);

    void Update()
    {
        if (player == null || fillImage == null) return;

        // Update radial fill amount (0.0 to 1.0)
        fillImage.fillAmount = player.MaxStamina > 0f ? (player.CurrentStamina / player.MaxStamina) : 0f;

        // Visual warning: red when locked out/cooling down, cyan/blue when usable
        fillImage.color = player.IsExhausted ? exhaustedColor : normalColor;
    }
}