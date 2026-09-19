using UnityEngine;

public class GameState : MonoBehaviour
{

    public float progression = 0;
    public float maxProgression = 200;

    public void AddProgress(float amount)
    {
        progression = Mathf.Clamp(progression + amount, 0f, maxProgression);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
