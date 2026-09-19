using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(
    fileName = "DungeonData",
    menuName = "Scriptable Objects/DungeonData"
)]
public class DungeonData : ScriptableObject
{
    [System.NonSerialized]
    public Scene currentCorridorScene;

     [System.NonSerialized]
    public Scene currentRoomScene;

    public bool HasCorridor =>
        currentCorridorScene.IsValid() &&
        currentCorridorScene.isLoaded;

    public void SetCorridor(Scene scene)
    {
        currentCorridorScene = scene;
    }

    public void ClearCorridor()
    {
        currentCorridorScene = default;
    }

    public bool HasCurrentRoom =>
        currentRoomScene.IsValid() &&
        currentRoomScene.isLoaded;

    public void SetCurrentRoom(Scene scene)
    {
        currentRoomScene = scene;
    }

    public void ClearCurrentRoom()
    {
        currentRoomScene = default;
    }

}