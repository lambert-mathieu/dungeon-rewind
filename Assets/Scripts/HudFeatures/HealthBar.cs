using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private float maxHealth = 100f;

    private float currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
        UpdateVisuals();
    }

    /// <summary>
    /// Deducts damage and updates the bar. Can be wired directly to a UI Button.
    /// </summary>
    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);
        UpdateVisuals();
    }

    /// <summary>
    /// Restores health up to maxHealth.
    /// </summary>
    public void Heal(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (fillImage != null && maxHealth > 0f)
        {
            fillImage.fillAmount = currentHealth / maxHealth;
        }
    }
}