using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;

    private void Update()
    {
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (fillImage == null || PlayerGlobal.State == null) return;

        float max = PlayerGlobal.State.MaxHealth;
        if (max > 0f)
        {
            fillImage.fillAmount = (float)PlayerGlobal.State.CurrentHealth / max;
        }
    }
}