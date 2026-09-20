namespace DungeonRewind.Rewind
{
    class ActiveRecorder : RewindRecorder<bool>
    {
        protected override void Apply(bool snapshot)
        {
            this.gameObject.SetActive(snapshot);
        }

        protected override bool Capture()
        {
            return this.gameObject.activeSelf;
        }
    }
}