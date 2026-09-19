namespace DungeonRewind.Rewind
{
    public interface IRewindable
    {
        void RecordTick(float time);
        void RewindTo(float time);
        void OnRewindBegin();
        void OnRewindEnd();
    }

    public interface IRewindSuspendable
    {
        void SuspendForRewind();
        void ResumeAfterRewind();
    }
}