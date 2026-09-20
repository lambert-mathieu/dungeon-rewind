using UnityEngine;

namespace DungeonRewind.Enemy {
    public sealed class EnemyFrank : EnemyLogic {
        private const int maxHealth = 40;
        private const float desiredAttackDistance = 12.0f;
        private const float attackDistanceTolerance = 2.0f;
        private const float aggroGetDistance = 30.0f;
        private const float aggroLoseDistance = 150.0f;
        private const float minAttackCooldown = 5.0f;
        private const float maxAttackCooldown = 6.0f;
        private const float moveSpeed = 5.0f;
        private const float attackWindupDuration = 0.6f;
        private const float fireballSpawnHeight = 1.5f;
        private const float fireballRadius = 0.25f;

        protected override int MaxHealth => maxHealth;
        protected override float DesiredAttackDistance => desiredAttackDistance;
        protected override float AttackDistanceTolerance => attackDistanceTolerance;
        protected override float AggroGetDistance => aggroGetDistance;
        protected override float AggroLoseDistance => aggroLoseDistance;
        protected override float MinAttackCooldown => minAttackCooldown;
        protected override float MaxAttackCooldown => maxAttackCooldown;
        protected override float MoveSpeed => moveSpeed;
        protected override float PrepareAttackDuration => attackWindupDuration;

        protected override void PerformAttack() {
            Vector3 spawnPosition = transform.position + Vector3.up * fireballSpawnHeight;
            Vector3 directionToPlayer = (PlayerGlobal.PlayerTransform.position - spawnPosition).normalized;

            GameObject fireball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fireball.name = "Fireball";
            fireball.transform.SetPositionAndRotation(spawnPosition, Quaternion.LookRotation(directionToPlayer));
            fireball.transform.localScale = Vector3.one * fireballRadius * 2f;
        }
    }
}
