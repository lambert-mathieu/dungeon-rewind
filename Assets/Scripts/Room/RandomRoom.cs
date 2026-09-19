using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RandomRoom", menuName = "Scriptable Objects/RandomRoom")]
public class RandomRoom : ScriptableObject
{
    public GameObject room1;
    public GameObject room2;
    public GameObject room3;

    public List<GameObject> gameObjects;
}
