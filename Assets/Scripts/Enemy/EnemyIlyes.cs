using UnityEngine;

namespace DungeonRewind.Enemy {
    public sealed class EnemyIlyes : EnemyLogic {
        private const int maxHealth = 50;
        private const float desiredAttackDistance = 2.4f;
        private const float attackDistanceTolerance = 1.2f;
        private const float aggroGetDistance = 20.0f;
        private const float aggroLoseDistance = 150.0f;
        private const float minAttackCooldown = 2.0f;
        private const float maxAttackCooldown = 999.0f;
        private const float moveSpeed = 6.0f;
        private const float attackWindupDuration = 0.6f;
        private const float meleeDetectionRange = 4.2f;
        private const int meleeDamage = 10;

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
            float distanceToPlayer = Vector3.Distance(transform.position, PlayerGlobal.PlayerTransform.position);
            if (distanceToPlayer <= meleeDetectionRange) {
                PlayerGlobal.Instance.TakeDamage(meleeDamage, false);
            }
        }
    }
}
