namespace DungeonRewind.Combat {
    public interface IDamageable {
        void TakeDamage(int amount, bool causedByRewindMagic);
    }
}
