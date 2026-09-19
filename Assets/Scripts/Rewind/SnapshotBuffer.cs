namespace DungeonRewind.Rewind {
    public readonly struct Sample<T> {
        public readonly float Timestamp;
        public readonly T Value;

        public Sample(float timestamp, T value) {
            Timestamp = timestamp;
            Value = value;
        }
    }

    public class SnapshotBuffer<T> where T : struct {
        private readonly Sample<T>[] samples;
        private int count;
        private int head;

        public SnapshotBuffer(int capacity) {
            samples = new Sample<T>[capacity];
        }

        public int Count => count;
        public bool IsEmpty() => count == 0;

        public void Push(float timestamp, T value) {
            samples[head] = new Sample<T>(timestamp, value);
            head = (head + 1) % samples.Length;

            if (count < samples.Length) {
                count++;
            }
        }

        public bool TryFindBracket(float time, out Sample<T> from, out Sample<T> to, out float t) {
            from = default;
            to = default;
            t = 0.0f;

            if (count == 0) {
                return false;
            }

            if (count == 1 || time <= GetSample(0).Timestamp) {
                from = GetSample(0);
                to = from;
                return true;
            }

            if (time >= GetSample(count - 1).Timestamp) {
                from = GetSample(count - 1);
                to = from;
                return true;
            }

            int low = 0;
            int high = count - 1;

            while (low < high) {
                int middle = (low + high + 1) / 2;

                if (GetSample(middle).Timestamp <= time) {
                    low = middle;
                } else {
                    high = middle - 1;
                }
            }

            from = GetSample(low);
            to = GetSample(low + 1);

            float span = to.Timestamp - from.Timestamp;
            t = span > 0.0f ? (time - from.Timestamp) / span : 0.0f;

            return true;
        }

        public void DiscardNewerThan(float time) {
            while (count > 0 && GetSample(count - 1).Timestamp > time) {
                count--;
                head = (head - 1 + samples.Length) % samples.Length;
            }
        }

        public void Clear() {
            count = 0;
            head = 0;
        }

        private Sample<T> GetSample(int index) => samples[(head - count + index + samples.Length) % samples.Length];
        public float LatestSampleTime() => count == 0 ? 0.0f : GetSample(count - 1).Timestamp;
    }
}
