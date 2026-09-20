using UnityEngine;

namespace DungeonRewind.Rewind {
    public abstract class RewindRecorder<TSnapshot> : MonoBehaviour, IRewindable where TSnapshot : struct {
        private float historyDuration = 5.0f;
        [SerializeField, Min(1.0f)] private float recordRate = 30.0f;

        private SnapshotBuffer<TSnapshot> buffer;
        private float recordInterval;
        private float nextRecordTime;
        private float lastSampledTime;

        protected TSnapshot LastAppliedSnapshot { get; private set; }

        protected abstract TSnapshot Capture();
        protected abstract void Apply(TSnapshot snapshot);

        protected virtual TSnapshot Interpolate(TSnapshot from, TSnapshot to, float t) => from;

        protected virtual void Initialize() {}
        protected virtual void OnRewindBeginInternal() { }
        protected virtual void OnRewindEndInternal(TSnapshot finalSnapshot) { }

        private void Awake() {
            EnsureInitialized();
        }

        private void EnsureInitialized() {
            if (buffer != null) {
                return;
            }

            int capacity = Mathf.CeilToInt(historyDuration * recordRate) + 1;

            buffer = new SnapshotBuffer<TSnapshot>(capacity);
            recordInterval = 1.0f / recordRate;
            Initialize();
        }

        public void RecordTick(float time) {
            EnsureInitialized();

            if (time < nextRecordTime) {
                return;
            }

            buffer.Push(time, Capture());
            nextRecordTime += recordInterval;

            if (nextRecordTime < time) {
                nextRecordTime = time + recordInterval;
            }
        }

        public void RewindTo(float time) {
            EnsureInitialized();
            lastSampledTime = time;

            if (!buffer.TryFindBracket(time, out Sample<TSnapshot> from, out Sample<TSnapshot> to, out float t)) {
                return;
            }

            LastAppliedSnapshot = Interpolate(from.Value, to.Value, t);
            Apply(LastAppliedSnapshot);
        }

        public void OnRewindBegin(float time) {
            EnsureInitialized();
            lastSampledTime = time;
            LastAppliedSnapshot = Capture();
            buffer.Push(lastSampledTime, LastAppliedSnapshot);
            OnRewindBeginInternal();
        }

        public void OnRewindEnd() {
            buffer.DiscardNewerThan(lastSampledTime);
            nextRecordTime = lastSampledTime;
            OnRewindEndInternal(LastAppliedSnapshot);
        }
    }
}
