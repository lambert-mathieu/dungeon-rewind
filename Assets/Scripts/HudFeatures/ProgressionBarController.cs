using UnityEngine;

public class ProgressionBarController : MonoBehaviour
{
    public GameState gameState;
    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (gameState == null) return;

        // Mask width directly expands from 0 to maxProgression to reveal purple fill
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, gameState.progression);
    }
}