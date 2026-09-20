using UnityEngine;

namespace DungeonRewind.Combat {
    public static class DamageableLookup {
        public static bool TryGetDamageable(Collider other, out IDamageable damageable) {
            if (other.TryGetComponent(out damageable)) {
                return true;
            }

            return other.attachedRigidbody != null && other.attachedRigidbody.TryGetComponent(out damageable);
        }
    }
}
