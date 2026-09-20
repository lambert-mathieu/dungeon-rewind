using UnityEngine;

namespace DungeonRewind.Enemy {
    public sealed class EnemyMaxime : EnemyLogic {
        private const int maxHealth = 100;
        private const float desiredAttackDistance = 7.5f;
        private const float attackDistanceTolerance = 1.0f;
        private const float aggroGetDistance = 15.0f;
        private const float aggroLoseDistance = 150.0f;
        private const float minAttackCooldown = 9.0f;
        private const float maxAttackCooldown = 10.0f;
        private const float moveSpeed = 5.5f;
        private const float attackWindupDuration = 0.7f;
        private const float spikeHeight = 2.0f;
        private const float spikeRadius = 0.3f;

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
            Vector3 spawnPosition = transform.position + Vector3.up * (spikeHeight * 0.5f);

            GameObject spike = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spike.name = "GroundSpike";
            spike.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
            spike.transform.localScale = new Vector3(spikeRadius, spikeHeight * 0.5f, spikeRadius);
        }
    }
}
