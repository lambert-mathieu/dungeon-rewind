using UnityEngine;

public class InitPlayer : MonoBehaviour
{
    public static PlayerData Data { get; private set; }

    [SerializeField] private PlayerData playerData;
    [SerializeField] private GameObject player;

    private void OnEnable()
    {
        playerData.Register(player);
        Data = playerData;
    }

    private void OnDisable()
    {
        playerData.Unregister(player);

        if (Data == playerData)
        {
            Data = null;
        }
    }
}