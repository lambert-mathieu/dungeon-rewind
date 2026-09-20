using UnityEngine;

namespace DungeonRewind.Rewind.Recorders
{
    public readonly struct RigidbodySnapshot
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly Vector3 LinearVelocity;
        public readonly Vector3 AngularVelocity;

        public RigidbodySnapshot(Vector3 position, Quaternion rotation, Vector3 linearVelocity, Vector3 angularVelocity)
        {
            Position = position;
            Rotation = rotation;
            LinearVelocity = linearVelocity;
            AngularVelocity = angularVelocity;
        }
    }

    [RequireComponent(typeof(Rigidbody))]
    public class RigidbodyRecorder : RewindRecorder<RigidbodySnapshot>, IRewindSuspendable
    {
        private const float minSweepDistance = 0.0001f;
        private const float minApproachRate = 0.01f;

        [SerializeField, Min(0.0f)] private float collisionSkin = 0.05f;

        private Rigidbody cachedRigidbody;
        private Transform cachedTransform;
        private bool wasKinematic;
        private RigidbodyInterpolation previousInterpolationMode;

        private bool isBlocked;

        protected override void Initialize()
        {
            cachedRigidbody = GetComponent<Rigidbody>();
            cachedTransform = transform;
        }

        protected override RigidbodySnapshot Capture()
        {
            return new RigidbodySnapshot(cachedTransform.position, cachedTransform.rotation, cachedRigidbody.linearVelocity, cachedRigidbody.angularVelocity);
        }

        protected override void OnRewindBeginInternal()
        {
            isBlocked = false;
        }

        protected override void OnRewindEndInternal(RigidbodySnapshot snapshot)
        {
            cachedRigidbody.linearVelocity = snapshot.LinearVelocity;
            cachedRigidbody.angularVelocity = snapshot.AngularVelocity;
        }

        protected override void Apply(RigidbodySnapshot snapshot)
        {
            Vector3 resolvedPosition = ResolveBlockedPosition(snapshot.Position);

            cachedRigidbody.position = resolvedPosition;
            cachedRigidbody.rotation = snapshot.Rotation;
            cachedTransform.SetPositionAndRotation(resolvedPosition, snapshot.Rotation);
        }

        private Vector3 ResolveBlockedPosition(Vector3 targetPosition)
        {
            Vector3 currentPosition = cachedRigidbody.position;
            Vector3 delta = targetPosition - currentPosition;
            float distance = delta.magnitude;

            if (distance <= minSweepDistance)
            {
                isBlocked = false;
                return targetPosition;
            }

            Vector3 direction = delta / distance;
            isBlocked = cachedRigidbody.SweepTest(direction, out RaycastHit hit, distance, QueryTriggerInteraction.Ignore);

            if (!isBlocked)
            {
                return targetPosition;
            }

            float approachRate = Mathf.Max(-Vector3.Dot(direction, hit.normal), minApproachRate);
            float allowedDistance = Mathf.Max(hit.distance - collisionSkin / approachRate, 0.0f);

            return currentPosition + direction * allowedDistance;
        }

        protected override RigidbodySnapshot Interpolate(RigidbodySnapshot from, RigidbodySnapshot to, float t)
        {
            return new RigidbodySnapshot(
                Vector3.Lerp(from.Position, to.Position, t),
                Quaternion.Slerp(from.Rotation, to.Rotation, t),
                Vector3.Lerp(from.LinearVelocity, to.LinearVelocity, t),
                Vector3.Lerp(from.AngularVelocity, to.AngularVelocity, t)
            );
        }

        public void SuspendForRewind()
        {
            wasKinematic = cachedRigidbody.isKinematic;
            previousInterpolationMode = cachedRigidbody.interpolation;
            cachedRigidbody.interpolation = RigidbodyInterpolation.None;
            cachedRigidbody.isKinematic = true;
        }

        public void ResumeAfterRewind()
        {
            Physics.SyncTransforms();
            cachedRigidbody.isKinematic = wasKinematic;
            cachedRigidbody.interpolation = previousInterpolationMode;
        }
    }
}
