using UnityEngine;

public class CloseRoom : MonoBehaviour
{
    [SerializeField] private PlayerData playerData;
    [SerializeField] private GameObject door;

    [Header("Closing")]
    [System.NonSerialized] private float minCloseDistance = 5f;
    [System.NonSerialized] private float maxCloseDistance = 12f;

    private bool isClosed = false;

    private void Update()
    {
        if (isClosed)
            return;

        if (playerData == null || playerData.playerTransform == null)
            return;

        if (door == null)
            return;

        Vector3 playerPosition =
            playerData.playerTransform.position;

        // Convert player position into the door's local space.
        // This means the check still works if the room is rotated.
        Vector3 playerRelativeToDoor =
            door.transform.InverseTransformPoint(playerPosition);

        // Player must be on the negative Z side of the door.
        bool playerIsPastDoor =
            playerRelativeToDoor.z < 0f;

        float distanceFromDoor =
            Vector3.Distance(
                playerPosition,
                door.transform.position
            );

        Debug.DrawLine(
            playerPosition,
            door.transform.position,
            Color.red
        );

        // Door only closes inside this distance window.
        //
        // Too close:
        //     Don't close yet.
        //
        // Correct distance:
        //     Close.
        //
        // Extremely far away:
        //     Don't close. This prevents the spawn-position bug.
        if (
            playerIsPastDoor &&
            distanceFromDoor >= minCloseDistance &&
            distanceFromDoor <= maxCloseDistance
        )
        {
            CloseDoor();
        }
    }

    private void CloseDoor()
    {
        door.transform.localPosition = new Vector3(0f, 2.88f, 19.96f);;

        isClosed = true;

        Debug.Log("Door closed.");
    }
}