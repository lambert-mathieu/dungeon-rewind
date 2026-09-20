using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StaminaWheelController : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float drainDuration = 1.5f;

    private Coroutine drainRoutine;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        // Hidden by default
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// Call this from a UI Button's OnClick() event.
    /// </summary>
    public void DepleteStamina()
    {
        if (drainRoutine != null)
            StopCoroutine(drainRoutine);

        drainRoutine = StartCoroutine(DrainRoutine());
    }

    private IEnumerator DrainRoutine()
    {
        // Show wheel and reset fill to full
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        if (fillImage != null) fillImage.fillAmount = 1f;

        float elapsed = 0f;

        while (elapsed < drainDuration)
        {
            elapsed += Time.deltaTime;
            if (fillImage != null)
            {
                fillImage.fillAmount = Mathf.Lerp(1f, 0f, elapsed / drainDuration);
            }
            yield return null;
        }

        if (fillImage != null) fillImage.fillAmount = 0f;

        // Hide wheel when depleted
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }
}