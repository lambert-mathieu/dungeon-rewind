using NUnit.Framework;
using UnityEngine;

namespace DungeonRewind.Rewind
{
    public class RewindableObject : MonoBehaviour
    {
        // [SerializeField] 
        private float baseRewindSpeed = 0f;
        // [SerializeField]
         private float baseRewindAcceleration = 0.5f;
        // [SerializeField]
        private float rewindJerk = 2.5f;
        // [SerializeField]
         private float maxRewindSpeed = 10.0f;

        private IRewindable[] recorders;
        private IRewindSuspendable[] suspendables;

        private float rewindSpeed;
        private float rewindAcceleration;
        private float rewindTime;
        private bool isRewinding;

        public bool IsRewinding => isRewinding;

        private void Awake()
        {
            recorders = GetComponentsInChildren<IRewindable>();
            suspendables = GetComponentsInChildren<IRewindSuspendable>();
        }

        public void BeginRewind()
        {
            Assert.IsFalse(isRewinding);
            isRewinding = true;
            rewindTime = Time.time;
            rewindSpeed = baseRewindSpeed;
            rewindAcceleration = baseRewindAcceleration;

            foreach (IRewindable rec in recorders)
            {
                rec.OnRewindBegin();
            }
            foreach(IRewindSuspendable sus in suspendables)
            {
                sus.SuspendForRewind();
            }
        }
        public void EndRewind()
        {
            Assert.IsTrue(isRewinding);
            foreach(IRewindSuspendable sus in suspendables)
            {
                sus.ResumeAfterRewind();
            }
            foreach (IRewindable rec in recorders)
            {
                rec.OnRewindEnd();
            }
            isRewinding = false;
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
                foreach(IRewindable rec in recorders)
                {
                   rec.RecordTick(Time.time);
                }
            }
        }
    }
}
