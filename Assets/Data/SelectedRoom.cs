using UnityEngine;

[CreateAssetMenu(
    fileName = "SelectedRoom",
    menuName = "Scriptable Objects/SelectedRoom"
)]
public class SelectedRoom : ScriptableObject
{
    [System.NonSerialized] public string roomName;
}