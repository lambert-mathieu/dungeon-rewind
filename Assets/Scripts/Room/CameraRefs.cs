using UnityEngine;

public class CameraRefs : MonoBehaviour
{
    public Camera playerCamera;
    public Camera armCamera;
    public Camera graphCamera;


    public void Awake()
    {
        playerCamera.gameObject.SetActive(true);
        armCamera.gameObject.SetActive(true);
        graphCamera.gameObject.SetActive(false);
    }

}