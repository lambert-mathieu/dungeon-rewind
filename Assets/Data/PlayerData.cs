using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerData",
    menuName = "Scriptable Objects/PlayerData"
)]
public class PlayerData : ScriptableObject
{
    [System.NonSerialized]
    public GameObject player;

    [System.NonSerialized]
    public Transform playerTransform;

    public bool IsReady => player != null;

    public void Register(GameObject playerRef)
    {
        player = playerRef;
        playerTransform = playerRef.transform;
    }

    public void Unregister(GameObject playerRef)
    {
        if (player != playerRef)
            return;

        player = null;
        playerTransform = null;
    }
}