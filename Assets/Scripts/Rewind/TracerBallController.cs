using DungeonRewind.Rewind;
using UnityEngine;

public class TracerBallController : RewindRecorder<float>, IRewindSuspendable
{
    public float speed = 1;
    public float R = 10;
    
    private bool active = true;

    private float phase;

    public void ResumeAfterRewind()
    {
        active = true;
    }

    public void SuspendForRewind()
    {
        active = false;
    }

    protected override void Apply(float snapshot)
    {
        phase = snapshot;
    }

    protected override float Capture()
    {
        return phase;
    }

    protected override float Interpolate(float from, float to, float t)
    {
        return Mathf.Lerp(from, to, t);
    }

    void Start()
    {
    }

    void Update()
    {
        if (!active)
        {
            return;
        }

        phase += Time.deltaTime * speed;
        transform.position = new Vector3(
            R * Mathf.Cos(phase),
            5.0f,
            R * Mathf.Sin(phase)
        );
    }
}
