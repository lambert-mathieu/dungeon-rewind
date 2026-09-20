using System.Collections;
using DungeonRewind.Rewind;
using UnityEngine;
using UnityEngine.UI;

public class ScreenEffectsController : MonoBehaviour
{
    [SerializeField] private Image damageFlashImage;
    [SerializeField] private Image rewindTintImage;
    private Color damageFlashColor = new Color(0.7960784f, 0.2705882f, 0.3686275f, 0.4f);
    private Color rewindTintColor = new Color(0.5568628f, 0.2392157f, 0.7450981f, 0.3f);
    private const float damageFlashDuration = 0.25f;

    private Coroutine damageFlashRoutine;

    private void Awake()
    {
        SetImageAlpha(damageFlashImage, 0f);
        SetImageAlpha(rewindTintImage, 0f);
    }

    private void OnEnable()
    {
        PlayerGlobal.PlayerDamaged += HandlePlayerDamaged;
        GlobalRewinder.RewindStateChanged += HandleRewindStateChanged;
    }

    private void OnDisable()
    {
        PlayerGlobal.PlayerDamaged -= HandlePlayerDamaged;
        GlobalRewinder.RewindStateChanged -= HandleRewindStateChanged;
    }

    private void HandlePlayerDamaged()
    {
        if (damageFlashRoutine != null)
        {
            StopCoroutine(damageFlashRoutine);
        }

        damageFlashRoutine = StartCoroutine(DamageFlashRoutine());
    }

    private IEnumerator DamageFlashRoutine()
    {
        damageFlashImage.color = damageFlashColor;
        yield return new WaitForSeconds(damageFlashDuration);
        SetImageAlpha(damageFlashImage, 0f);
        damageFlashRoutine = null;
    }

    private void HandleRewindStateChanged(bool isRewinding)
    {
        rewindTintImage.color = isRewinding ? rewindTintColor : SetAlpha(rewindTintColor, 0f);
    }

    private static void SetImageAlpha(Image image, float alpha)
    {
        image.color = SetAlpha(image.color, alpha);
    }

    private static Color SetAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
