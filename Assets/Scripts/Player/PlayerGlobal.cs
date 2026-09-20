using UnityEngine;

public class PlayerGlobal : MonoBehaviour {
    public static Transform PlayerTransform { get; private set; }

    private void OnEnable() {
        PlayerTransform = transform;
    }

    private void OnDisable() {
        if (PlayerTransform == transform) {
            PlayerTransform = null;
        }
    }
}
