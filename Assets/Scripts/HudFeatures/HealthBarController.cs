using UnityEngine;

public class HealthBarController : MonoBehaviour
{
    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, PlayerGlobal.State.CurrentHealth);
    }
}
