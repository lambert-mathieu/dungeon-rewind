using UnityEngine;

public class MinimapCameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform playerTransform;

    [Header("Camera Settings")]
    [SerializeField] private float height = 20f;
    [SerializeField] private bool rotateWithPlayer = false;

    private void LateUpdate()
    {
        if (playerTransform == null) return;

        // Follow player's X and Z position, keep a fixed height on Y
        Vector3 targetPosition = new Vector3(playerTransform.position.x, height, playerTransform.position.z);
        transform.position = targetPosition;

        // Optionally rotate the camera as the player turns
        if (rotateWithPlayer)
        {
            transform.rotation = Quaternion.Euler(90f, playerTransform.eulerAngles.y, 0f);
        }
        else
        {
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
}