using System;
using System.Collections.Generic;
using DungeonRewind.Combat;
using DungeonRewind.Rewind;
using UnityEngine;

namespace DungeonRewind.Enemy {
    public sealed class GroundSpike : MonoBehaviour, IRewindSuspendable {
        [SerializeField] private Transform spikeTemplate;
        [SerializeField, Min(0.01f)] private float growthSpeed = 10.0f;
        [SerializeField, Min(0.01f)] private float maxLength = 10.0f;
        [SerializeField, Min(0.0f)] private float holdDuration = 2.0f;
        [SerializeField, Min(0.01f)] private float spikeSpacing = 1.2f;
        [SerializeField] private int damage = 15;

        private readonly List<GameObject> spikeInstances = new List<GameObject>();
        private readonly HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();
        private Transform owner;
        private Action onOwnerHit;
        private float elapsedTime;
        private float currentLength;
        private bool isRewinding;

        public float Lifetime => GrowthDuration + holdDuration;

        private float GrowthDuration => maxLength / growthSpeed;

        public void Launch(Transform spikeOwner, Action ownerHitCallback) {
            owner = spikeOwner;
            onOwnerHit = ownerHitCallback;
        }

        private void Update() {
            if (!isRewinding) {
                elapsedTime += Time.deltaTime;
                if (elapsedTime > Lifetime) {
                    Destroy(gameObject);
                    return;
                }

                currentLength = Mathf.Min(elapsedTime * growthSpeed, maxLength);
                RevealSpikesUpToCurrentLength();
            }

            DamageOverlappingTargets();
        }

        private void RevealSpikesUpToCurrentLength() {
            int targetCount = Mathf.FloorToInt(currentLength / spikeSpacing) + 1;

            while (spikeInstances.Count < targetCount) {
                Transform spike = Instantiate(spikeTemplate, transform);
                spike.localPosition = new Vector3(0.0f, spikeTemplate.localPosition.y, spikeInstances.Count * spikeSpacing);
                spike.gameObject.SetActive(true);
                spikeInstances.Add(spike.gameObject);
            }
        }

        private void DamageOverlappingTargets() {
            Vector3 spikeSize = spikeTemplate.localScale;
            Vector3 localCenter = new Vector3(0.0f, spikeTemplate.localPosition.y, currentLength * 0.5f);
            Vector3 halfExtents = new Vector3(spikeSize.x * 0.5f, spikeSize.y * 0.5f, (currentLength + spikeSize.z) * 0.5f);
            Collider[] overlapping = Physics.OverlapBox(transform.TransformPoint(localCenter), halfExtents, transform.rotation, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            foreach (Collider hitCollider in overlapping) {
                TryDamage(hitCollider);
            }
        }

        private void TryDamage(Collider hitCollider) {
            bool isOwner = IsOwnedBy(hitCollider);
            if (!isRewinding && isOwner) {
                return;
            }

            if (!DamageableLookup.TryGetDamageable(hitCollider, out IDamageable damageable)) {
                return;
            }

            if (!damagedTargets.Add(damageable)) {
                return;
            }

            damageable.TakeDamage(damage, isRewinding);

            if (isOwner) {
                onOwnerHit?.Invoke();
            }
        }

        private bool IsOwnedBy(Collider hitCollider) {
            return owner != null && hitCollider.transform.IsChildOf(owner);
        }

        public void SuspendForRewind() {
            isRewinding = true;
        }

        public void ResumeAfterRewind() {
            isRewinding = false;
        }
    }
}
