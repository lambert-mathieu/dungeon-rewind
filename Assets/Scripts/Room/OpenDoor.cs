using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEngine;
using UnityEngine.InputSystem;

public class OpenDoor : MonoBehaviour
{
    public GameObject door = null;
    public Transform playerTransform = null;
    private float minDistance = 5;
    private GameObject[] dungeonRooms;
    private List<GameObject> instantiatedRooms = new List<GameObject>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       dungeonRooms = Resources.LoadAll<GameObject>("Rooms/");

       if(dungeonRooms != null && dungeonRooms.Length != 0)
        {
        }
        else
        {
            if(dungeonRooms == null)
            {
                Debug.LogError("DungeonRooms is null");
            }
            else
            {
                 Debug.LogError("DungeonRooms is empty");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        float distancePlayerDoor = (playerTransform.position - door.transform.position).magnitude;
        Debug.DrawLine(playerTransform.position, door.transform.position);
        //Debug.Log(" Distance between player and door: " + distancePlayerDoor);
        
        if (Keyboard.current.eKey.wasPressedThisFrame && distancePlayerDoor <= minDistance)
        {
            Vector3 newRoomPos = new Vector3(0f, 0f, -10f) + transform.position;
            Quaternion newRoomRotation = new Quaternion(0f, 1f, 0f, 0f); //180
            GameObject newRoom = Instantiate(dungeonRooms[0], newRoomPos, newRoomRotation);
            if (newRoom.TryGetComponent(out OpenDoor newRoomWithDoor))
            {
               newRoomWithDoor.playerTransform = this.playerTransform;
               instantiatedRooms.Add(newRoom);
            }

            door.transform.localPosition = new Vector3(0, 5, 0);
        }

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            Destroy(instantiatedRooms[0]);
        }
    }
}