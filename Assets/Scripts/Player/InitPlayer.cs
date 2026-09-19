using UnityEngine;

public class InitPlayer : MonoBehaviour
{
    [SerializeField] private PlayerData playerData;
    [SerializeField] private GameObject player;

    private void OnEnable()
    {
        playerData.Register(player);
    }

    private void OnDisable()
    {
        playerData.Unregister(player);
    }
}