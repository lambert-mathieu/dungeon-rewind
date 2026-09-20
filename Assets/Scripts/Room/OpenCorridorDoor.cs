using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class OpenCorridorDoor : MonoBehaviour
{
    public GameObject entranceDoor = null;
    public GameObject exitDoor = null;

    [SerializeField] private PlayerData playerData;
    [SerializeField] private DungeonData dungeonData;
    [SerializeField] private Transform currentExit;

    [SerializeField] private string mapSceneName = "LevelMapScene";
    private bool isMapOpen = false;

    private float minDistance = 5f;
    private bool isLoadingRoom = false;

    private void OnEnable()
{
    HexMapNode.OnRoomNodeSelected += HandleRoomSelected;
}

private void OnDisable()
{
    HexMapNode.OnRoomNodeSelected -= HandleRoomSelected;
}

private async void HandleRoomSelected(string nextRoomName)
{
    Scene mapScene = SceneManager.GetSceneByName(mapSceneName);
    if (mapScene.isLoaded)
    {
        AsyncOperation unload = SceneManager.UnloadSceneAsync(mapScene);
        while (unload != null && !unload.isDone)
            await System.Threading.Tasks.Task.Yield();
    }

    isMapOpen = false;
    LoadNextRoom(nextRoomName);

    exitDoor.transform.localPosition = new Vector3(-7.65f, 6f, 0);
    entranceDoor.transform.localPosition = new Vector3(7.39f, 2.54f, 0);
}

    public async void LoadNextRoom(string sceneName)
    {
        if (isLoadingRoom)
            return;

        isLoadingRoom = true;

        // Unload the previous ROOM first
        if (dungeonData.HasCurrentRoom)
        {
            AsyncOperation unloadOperation =
                SceneManager.UnloadSceneAsync(dungeonData.currentRoomScene);

            if (unloadOperation != null)
            {
                while (!unloadOperation.isDone)
                {
                    await System.Threading.Tasks.Task.Yield();
                }
            }

            dungeonData.ClearCurrentRoom();
        }

        // Load the next room
        AsyncOperation loadOperation =
            SceneManager.LoadSceneAsync(
                sceneName,
                LoadSceneMode.Additive
            );

        while (!loadOperation.isDone)
        {
            await System.Threading.Tasks.Task.Yield();
        }

        Scene newScene = SceneManager.GetSceneByName(sceneName);

        if (!newScene.IsValid() || !newScene.isLoaded)
        {
            Debug.LogError("Failed to load scene " + sceneName);
            isLoadingRoom = false;
            return;
        }

        GameObject[] roots = newScene.GetRootGameObjects();

        Transform entrance = null;
        Transform newExit = null;

        foreach (GameObject root in roots)
        {
            Transform[] children =
                root.GetComponentsInChildren<Transform>(true);

            foreach (Transform child in children)
            {
                if (child.CompareTag("Entrance"))
                    entrance = child;

                if (child.CompareTag("Exit"))
                    newExit = child;
            }
        }

        if (entrance == null)
        {
            Debug.LogError("No Entrance found in scene " + sceneName);
            isLoadingRoom = false;
            return;
        }

        if (newExit == null)
        {
            Debug.LogError("No Exit found in scene " + sceneName);
            isLoadingRoom = false;
            return;
        }

        if (currentExit == null)
        {
            Debug.LogError("Current Exit is null");
            isLoadingRoom = false;
            return;
        }

        // Rotate the new room so its entrance faces the corridor exit
        Quaternion targetEntranceRotation =
            currentExit.rotation *
            Quaternion.Euler(0f, 180f, 0f);

        Quaternion rotationOffset =
            targetEntranceRotation *
            Quaternion.Inverse(entrance.rotation);

        foreach (GameObject root in roots)
        {
            root.transform.rotation =
                rotationOffset * root.transform.rotation;
        }

        // Move AFTER rotating
        Vector3 offset =
            currentExit.position - entrance.position;

        foreach (GameObject root in roots)
        {
            root.transform.position += offset;
        }

        currentExit = newExit;

        // Remember this room so the next corridor can unload it
        dungeonData.SetCurrentRoom(newScene);

        isLoadingRoom = false;
    }

    void Update()
    {
        if (playerData.playerTransform == null)
            return;

        float distancePlayerDoor =
            Vector3.Distance(
                playerData.playerTransform.position,
                exitDoor.transform.position
            );

        Debug.DrawLine(
            playerData.playerTransform.position,
            exitDoor.transform.position
        );

        if (Keyboard.current.eKey.wasPressedThisFrame && distancePlayerDoor <= minDistance)
        {
            if (!isMapOpen && !isLoadingRoom)
            {
                isMapOpen = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                SceneManager.LoadSceneAsync(mapSceneName, LoadSceneMode.Additive);
            }
        }
    }
}