using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class OpenDoor : MonoBehaviour
{
    public GameObject door;

    [SerializeField] private DungeonData dungeonData;
    [SerializeField] private Transform currentExit;
    public Camera playerCamera;
    public Camera armCamera;
    public Camera graphCamera;


    private float minDistance = 5f;
    private bool isLoadingCorridor = false;

    public async void LoadCorridor()
    {
        if (isLoadingCorridor)
            return;

        isLoadingCorridor = true;

        string sceneName = "Corridor";

        // ------------------------------------
        // 1. Unload previous corridor
        // ------------------------------------

        if (dungeonData.HasCorridor)
        {
            AsyncOperation unloadOperation =
                SceneManager.UnloadSceneAsync(
                    dungeonData.currentCorridorScene
                );

            if (unloadOperation != null)
            {
                while (!unloadOperation.isDone)
                {
                    await System.Threading.Tasks.Task.Yield();
                }
            }

            dungeonData.ClearCorridor();
        }

        // ------------------------------------
        // 2. Load new corridor
        // ------------------------------------

        AsyncOperation loadOperation =
            SceneManager.LoadSceneAsync(
                sceneName,
                LoadSceneMode.Additive
            );

        while (!loadOperation.isDone)
        {
            await System.Threading.Tasks.Task.Yield();
        }

        // Because the old corridor was completely
        // unloaded, only one Corridor exists now.
        Scene newScene =
            SceneManager.GetSceneByName(sceneName);

        if (!newScene.IsValid() || !newScene.isLoaded)
        {
            Debug.LogError("Failed to load " + sceneName);
            isLoadingCorridor = false;
            return;
        }

        GameObject[] roots =
            newScene.GetRootGameObjects();

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
            Debug.LogError("No Entrance found in " + sceneName);
            isLoadingCorridor = false;
            return;
        }

        if (newExit == null)
        {
            Debug.LogError("No Exit found in " + sceneName);
            isLoadingCorridor = false;
            return;
        }

        if (currentExit == null)
        {
            Debug.LogError("Current Exit is null");
            isLoadingCorridor = false;
            return;
        }

        // ------------------------------------
        // 3. Align rotation
        // ------------------------------------

        Quaternion desiredEntranceRotation =
            currentExit.rotation *
            Quaternion.Euler(0f, 180f, 0f);

        Quaternion rotationDifference =
            desiredEntranceRotation *
            Quaternion.Inverse(entrance.rotation);

        foreach (GameObject root in roots)
        {
            root.transform.rotation =
                rotationDifference *
                root.transform.rotation;
        }

        // ------------------------------------
        // 4. Align position AFTER rotation
        // ------------------------------------

        Vector3 offset =
            currentExit.position -
            entrance.position;

        foreach (GameObject root in roots)
        {
            root.transform.position += offset;
        }

        // ------------------------------------
        // 5. Remember this corridor globally
        // ------------------------------------

        dungeonData.SetCorridor(newScene);

        isLoadingCorridor = false;
    }

    private void Awake()
    {
        playerCamera.gameObject.SetActive(true);
        armCamera.gameObject.SetActive(true);
        graphCamera.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (PlayerGlobal.PlayerTransform == null)
            return;

        float distancePlayerDoor =
            Vector3.Distance(
                PlayerGlobal.PlayerTransform.position,
                door.transform.position
            );

        Debug.DrawLine(
            PlayerGlobal.PlayerTransform.position,
            door.transform.position
        );

        if (Keyboard.current.eKey.wasPressedThisFrame && distancePlayerDoor <= minDistance)
        {

            LoadCorridor();
            door.transform.localPosition = new Vector3(0, 6, 0);
        }
    }
}