using UnityEngine;

namespace DungeonRewind.Enemy {
    [RequireComponent(typeof(Collider))]
    public class FrankAttack : MonoBehaviour {
        [SerializeField] private FrankProjectile projectilePrefab;
        [SerializeField] private FrankProjectile boringFrankProjectilePrefab;
        [SerializeField] private float targetHeightOffset = 5;
        [SerializeField] private float attackSpeed = 10f;
        [SerializeField] private int attackDamage = 15;

        private Collider ownerCollider;

        private bool nextProjectileIsBoring = true;

        private void Awake() {
            TryGetComponent(out ownerCollider);
        }

        public void OnAttack() {
            if (projectilePrefab == null || PlayerGlobal.PlayerTransform == null) {
                return;
            }


            Vector3 spawnPosition = ownerCollider.bounds.center;
            Vector3 targetPosition = PlayerGlobal.PlayerTransform.position + Vector3.up * targetHeightOffset;
            Vector3 direction = (targetPosition - spawnPosition).normalized;

            if (nextProjectileIsBoring)
            {
                FrankProjectile projectile = Instantiate(boringFrankProjectilePrefab, spawnPosition, Quaternion.LookRotation(direction));
                projectile.Launch(transform, direction, attackSpeed, attackDamage);
            }
            else
            {
                FrankProjectile projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.LookRotation(direction));
                projectile.Launch(transform, direction, attackSpeed, attackDamage);
            }
            nextProjectileIsBoring = !nextProjectileIsBoring;
        }
    }
}
