using System;
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
    CameraRefs cameraRefs = null;

    private bool isMapOpen = false;

    private float minDistance = 5f;
    private bool isLoadingRoom = false;

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

    // void OpenDoor()
    // {
    //      // Call in graph
    //     if (!string.IsNullOrEmpty(selectedRoom.roomName))
    //     {
    //         Debug.Log("Room name: " + selectedRoom.roomName);
    //         LoadNextRoom(selectedRoom.roomName);

    //         exitDoor.transform.localPosition = new Vector3(-7.65f, 6f, 0);
    //         entranceDoor.transform.localPosition = new Vector3(7.39f, 2.54f, 0);

    //     }

    //     else
    //     {
    //         Debug.Log("Room name was empty");
    //     }
    // }

    void Start()
    {
        Scene scene = SceneManager.GetSceneByName("DungeonGenerationScene");
        if (!scene.isLoaded)
                {
                    Debug.LogWarning("PlayerScene is not loaded.");
                    return;
                }


                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    cameraRefs = root.GetComponentInChildren<CameraRefs>(true);

                    if (cameraRefs != null)
                        break;
                }

                if (cameraRefs == null)
                {
                    Debug.LogWarning("CameraReferences was not found in PlayerScene.");
                    return;
                }
    }

    void Update()
    {

        if (playerData.playerTransform == null)
            return;

        float distancePlayerDoor = Vector3.Distance(playerData.playerTransform.position, exitDoor.transform.position);

        if (Keyboard.current.eKey.wasPressedThisFrame && distancePlayerDoor <= minDistance)
        {
            cameraRefs.playerCamera.gameObject.SetActive(false);
            cameraRefs.armCamera.gameObject.SetActive(false);
            cameraRefs.graphCamera.gameObject.SetActive(true);
        }
    }
}