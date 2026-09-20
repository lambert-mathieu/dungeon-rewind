using System.Collections.Generic;
using UnityEngine;

namespace DungeonRewind.Enemy {
    public sealed class EnemyMaxime : EnemyLogic {
        private const int maxHealth = 225;
        private const float desiredAttackDistanceFar = 18.0f;
        private const float desiredAttackDistanceNear = 10.0f;
        private const float attackDistanceTolerance = 0.5f;
        private const float aggroGetDistance = 20.0f;
        private const float aggroLoseDistance = 150.0f;
        private const float minAttackCooldown = 2.5f;
        private const float maxAttackCooldown = 7.0f;
        private const float moveSpeed = 7.9f;
        private const float attackWindupDuration = 0.7f;
        private const int spikeDirectionCount = 6;
        private const float spikeGapFromEnemy = 2.5f;
        private const float spikeClearanceDelay = 0.1f;

        [SerializeField] private GroundSpike spikePrefab;

        private readonly List<GroundSpike> activeSpikes = new List<GroundSpike>();
        private Collider ownerCollider;
        private float currentDesiredAttackDistance = desiredAttackDistanceFar;

        protected override int MaxHealth => maxHealth;
        protected override float DesiredAttackDistance => currentDesiredAttackDistance;
        protected override float AttackDistanceTolerance => attackDistanceTolerance;
        protected override float AggroGetDistance => aggroGetDistance;
        protected override float AggroLoseDistance => aggroLoseDistance;
        protected override float MinAttackCooldown => minAttackCooldown;
        protected override float MaxAttackCooldown => maxAttackCooldown;
        protected override float MoveSpeed => moveSpeed;
        protected override float PrepareAttackDuration => attackWindupDuration;
        protected override float AttackPoseDuration => spikePrefab != null ? spikePrefab.Lifetime + spikeClearanceDelay : base.AttackPoseDuration;

        protected override void PerformAttack() {
            if (spikePrefab == null) {
                return;
            }

            if (ownerCollider == null) {
                TryGetComponent(out ownerCollider);
            }

            Vector3 groundCenter = new Vector3(transform.position.x, ownerCollider.bounds.min.y, transform.position.z);
            activeSpikes.Clear();
            float yawTowardsPlayer = GetYawTowardsPlayer();
            float angleStep = 360.0f / spikeDirectionCount;

            for (int i = 0; i < spikeDirectionCount; i++) {
                Quaternion spikeRotation = Quaternion.Euler(0.0f, yawTowardsPlayer + angleStep * i, 0.0f);
                Vector3 spawnPosition = groundCenter + spikeRotation * Vector3.forward * spikeGapFromEnemy;
                GroundSpike spike = Instantiate(spikePrefab, spawnPosition, spikeRotation);
                spike.Launch(transform, DespawnActiveSpikes);
                activeSpikes.Add(spike);
            }

            currentDesiredAttackDistance = currentDesiredAttackDistance == desiredAttackDistanceFar ? desiredAttackDistanceNear : desiredAttackDistanceFar;
        }

        private void DespawnActiveSpikes() {
            foreach (GroundSpike spike in activeSpikes) {
                if (spike != null) {
                    Destroy(spike.gameObject);
                }
            }

            activeSpikes.Clear();
        }

        private float GetYawTowardsPlayer() {
            if (PlayerGlobal.PlayerTransform == null) {
                return transform.eulerAngles.y;
            }

            Vector3 directionToPlayer = PlayerGlobal.PlayerTransform.position - transform.position;
            directionToPlayer.y = 0.0f;

            if (directionToPlayer.sqrMagnitude < 0.0001f) {
                return transform.eulerAngles.y;
            }

            return Quaternion.LookRotation(directionToPlayer).eulerAngles.y;
        }
    }
}
