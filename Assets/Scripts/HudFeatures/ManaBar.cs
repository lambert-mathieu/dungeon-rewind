using UnityEngine;
using UnityEngine.UI;
using DungeonRewind.Rewind;

public class ManaBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private GlobalRewinder globalRewinder;

    private void Awake()
    {
        if (globalRewinder == null)
        {
            globalRewinder = GlobalRewinder.Instance != null 
                ? GlobalRewinder.Instance 
                : FindAnyObjectByType<GlobalRewinder>();
        }
    }

    private void Update()
    {
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (fillImage == null || globalRewinder == null || globalRewinder.MaxRewindTime <= 0f) return;

        fillImage.fillAmount = globalRewinder.CurrentRewindTime / globalRewinder.MaxRewindTime;
    }
}