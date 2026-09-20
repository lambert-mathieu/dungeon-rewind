using UnityEngine;
using UnityEngine.Assertions;

namespace DungeonRewind.Rewind
{
    public class RewindableObject : MonoBehaviour
    {
        // [SerializeField] 
        private float baseRewindSpeed = 0.2f;
        // [SerializeField]
         private float baseRewindAcceleration = 0.5f;
        // [SerializeField]
        private float rewindJerk = 1.8f;
        // [SerializeField]
         private float maxRewindSpeed = 2.3f;

        private IRewindable[] recorders;
        private IRewindSuspendable[] suspendables;

        private float rewindSpeed;
        private float rewindAcceleration;
        private float rewindTime;
        private bool isRewinding;

        private float timelineTime;

        public bool IsRewinding => isRewinding;

        private void Awake()
        {
            recorders = GetComponentsInChildren<IRewindable>(true);
            suspendables = GetComponentsInChildren<IRewindSuspendable>(true);
        }

        public void BeginRewind()
        {
            if (isRewinding)
            {
                return; //skip
            }
            isRewinding = true;
            rewindTime = timelineTime;
            rewindSpeed = baseRewindSpeed;
            rewindAcceleration = baseRewindAcceleration;

            foreach (IRewindable rec in recorders)
            {
                rec.OnRewindBegin(timelineTime);
            }
            foreach(IRewindSuspendable sus in suspendables)
            {
                sus.SuspendForRewind();
            }
        }
        public void EndRewind()
        {
            if (!isRewinding)
            {
                return; //skip
            }
            foreach(IRewindSuspendable sus in suspendables)
            {
                sus.ResumeAfterRewind();
            }
            foreach (IRewindable rec in recorders)
            {
                rec.OnRewindEnd();
            }
            isRewinding = false;
            timelineTime = rewindTime;
        }

        private void Update()
        {
            if (isRewinding)
            {
                foreach(IRewindable rec in recorders)
                {
                    rec.RewindTo(rewindTime);
                }
                rewindTime -= Time.deltaTime * rewindSpeed;
                rewindSpeed = Mathf.Min(rewindSpeed + rewindAcceleration * Time.deltaTime, maxRewindSpeed);
                rewindAcceleration = rewindAcceleration + rewindJerk * Time.deltaTime;
            }
            else
            {
                timelineTime += Time.deltaTime;
                foreach(IRewindable rec in recorders)
                {
                   rec.RecordTick(timelineTime);
                }
            }
        }
    }
}
