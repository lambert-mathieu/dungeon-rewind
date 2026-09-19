using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    [Header ("UI References")]
    [SerializeField] private Slider slider;
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI energyText;

    [Header ("Visual Settings")]
    [SerializeField] private Color barColor = new Color(0.2f, 0.85f, 0.3f, 1f);

    private void Awake()
    {
        if (fillImage != null) 
        {
            fillImage.color = barColor;    
        }
    }

    public void SetMaxHealth(int maxHealth)
    {
        slider.maxValue = maxHealth;
        slider.value = maxHealth;
        UpdateText(maxHealth, (int)slider.maxValue);
    }

    public void SetHealth(int currentHealth)
    {
        slider.value = currentHealth;
        UpdateText(currentHealth, (int)slider.maxValue);
    }

    private void UpdateText(int current, int max)
    {
        if (energyText != null)
        {
            energyText.text = $"{current} / {max}";
        }
    }
}
