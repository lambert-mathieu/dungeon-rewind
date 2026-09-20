using UnityEngine;
using UnityEngine.UI;

public class ManaBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;

    [Header("Mana Settings")]
    [SerializeField] private float maxMana = 100f;
    [SerializeField] private float drainRate = 25f;  // Mana lost per second while rewinding
    [SerializeField] private float rechargeRate = 15f; // Mana gained per second while idling

    private float currentMana;
    private bool isRewinding;

    public bool HasMana => currentMana > 0f;

    private void Awake()
    {
        currentMana = maxMana;
        UpdateVisuals();
    }

    private void Update()
    {
        if (isRewinding)
        {
            // Drain mana while rewinding is active
            currentMana = Mathf.MoveTowards(currentMana, 0f, drainRate * Time.deltaTime);

            if (currentMana <= 0f)
            {
                isRewinding = false; // Stop rewinding if out of mana
            }
        }
        else if (currentMana < maxMana)
        {
            // Passively refill mana when not rewinding
            currentMana = Mathf.MoveTowards(currentMana, maxMana, rechargeRate * Time.deltaTime);
        }

        UpdateVisuals();
    }

    /// <summary>
    /// Starts depleting mana. Wire to Rewind Button (Pointer Down) or call when rewind starts.
    /// </summary>
    public void StartRewind()
    {
        if (currentMana > 0f)
        {
            isRewinding = true;
        }
    }

    /// <summary>
    /// Stops depleting mana and allows recharge. Wire to Rewind Button (Pointer Up) or call when rewind ends.
    /// </summary>
    public void StopRewind()
    {
        isRewinding = false;
    }

    /// <summary>
    /// Instant flat deduction if you prefer a single button click instead of hold.
    /// </summary>
    public void ConsumeMana(float amount)
    {
        currentMana = Mathf.Clamp(currentMana - amount, 0f, maxMana);
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (fillImage != null && maxMana > 0f)
        {
            fillImage.fillAmount = currentMana / maxMana;
        }
    }
}