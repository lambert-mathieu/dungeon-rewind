using UnityEngine;

namespace DungeonRewind.Rewind.Recorders
{
    public readonly struct TransformSnapshot 
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;

        public TransformSnapshot(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }

    public class TransformRecorder : RewindRecorder<TransformSnapshot>
    {
        private Transform cachedTransform;

        protected override void Initialize()
        {
            cachedTransform = transform;
        }

        protected override TransformSnapshot Capture()
        {
            return new TransformSnapshot(cachedTransform.localPosition, cachedTransform.localRotation);
        }

        protected override void Apply(TransformSnapshot snapshot)
        {
            cachedTransform.SetLocalPositionAndRotation(snapshot.Position, snapshot.Rotation);
        }

        protected override TransformSnapshot Interpolate(TransformSnapshot from, TransformSnapshot to, float t)
        {
            return new TransformSnapshot(Vector3.Lerp(from.Position, to.Position, t), Quaternion.Slerp(from.Rotation, to.Rotation, t));
        }
    }
}
