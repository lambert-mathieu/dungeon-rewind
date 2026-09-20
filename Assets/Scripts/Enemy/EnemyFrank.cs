using UnityEngine;

namespace DungeonRewind.Enemy {
    [RequireComponent(typeof(FrankAttack))]
    public sealed class EnemyFrank : EnemyLogic {
        private const int maxHealth = 40;
        private const float desiredAttackDistance = 12.0f;
        private const float attackDistanceTolerance = 2.0f;
        private const float aggroGetDistance = 15.0f;
        private const float aggroLoseDistance = 150.0f;
        private const float minAttackCooldown = 5.0f;
        private const float maxAttackCooldown = 6.0f;
        private const float moveSpeed = 5.0f;
        private const float attackWindupDuration = 0.6f;

        private FrankAttack frankAttack;

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
            if (frankAttack == null) {
                TryGetComponent(out frankAttack);
            }

            frankAttack.OnAttack();
        }
    }
}
