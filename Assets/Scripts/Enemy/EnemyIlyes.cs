using UnityEngine;

namespace DungeonRewind.Enemy {
    public sealed class EnemyIlyes : EnemyLogic {
        private const int maxHealth = 30;
        private const float desiredAttackDistance = 2.0f;
        private const float attackDistanceTolerance = 1.0f;
        private const float aggroGetDistance = 20.0f;
        private const float aggroLoseDistance = 100.0f;
        private const float minAttackCooldown = 4.0f;
        private const float maxAttackCooldown = 99.0f;
        private const float moveSpeed = 4.0f;
        private const float attackWindupDuration = 0.3f;
        private const float meleeHitSpawnDistance = 1.0f;
        private const float meleeHitSize = 0.5f;

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
            Vector3 directionToPlayer = (PlayerGlobal.PlayerTransform.position - transform.position).normalized;
            Vector3 spawnPosition = transform.position + directionToPlayer * meleeHitSpawnDistance;

            GameObject meleeHit = GameObject.CreatePrimitive(PrimitiveType.Cube);
            meleeHit.name = "MeleeHit";
            meleeHit.transform.SetPositionAndRotation(spawnPosition, Quaternion.LookRotation(directionToPlayer));
            meleeHit.transform.localScale = Vector3.one * meleeHitSize;
        }
    }
}
