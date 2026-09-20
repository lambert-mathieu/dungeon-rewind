using DungeonRewind.Combat;
using DungeonRewind.Rewind;
using UnityEngine;

namespace DungeonRewind.Enemy
{
    [RequireComponent(typeof(Rigidbody))]
    public class FrankProjectile : MonoBehaviour, IRewindSuspendable 
    {
        [SerializeField] private float lifetime = 60f;
        private int damage = 10;

        private Rigidbody rb;
        private Transform owner;
        private Vector3 velocity = new Vector3(1, 0, 0);
        private float remainingLifetime;
        private bool isRewinding;

        private void Awake()
        {
            TryGetComponent(out rb);
            rb.isKinematic = true;
            rb.useGravity = false;
            remainingLifetime = lifetime;
        }

        public void Launch(Transform projectileOwner, Vector3 direction, float speed, int attackDamage)
        {
            owner = projectileOwner;
            velocity = direction.normalized * speed;
            damage = attackDamage;
        }

        private void FixedUpdate()
        {
            if (isRewinding)
            {
                return;
            }

            rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
        }

        private void Update()
        {
            if (isRewinding)
            {
                return;
            }

            remainingLifetime -= Time.deltaTime;
            if (remainingLifetime <= 0f)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.isTrigger || IsFrankProjectile(other) || (!isRewinding && IsOwnedBy(other)))
            {
                return;
            }

            if (TryGetDamageable(other, out IDamageable damageable))
            {
                damageable.TakeDamage(damage);
            }

            Destroy(gameObject);
        }

        private bool IsOwnedBy(Collider other)
        {
            return owner != null && other.transform.IsChildOf(owner);
        }

        private static bool IsFrankProjectile(Collider other)
        {
            return other.TryGetComponent(out FrankProjectile _);
        }

        private static bool TryGetDamageable(Collider other, out IDamageable damageable)
        {
            if (other.TryGetComponent(out damageable))
            {
                return true;
            }

            return other.attachedRigidbody != null && other.attachedRigidbody.TryGetComponent(out damageable);
        }

        public void SuspendForRewind()
        {
            isRewinding = true;
        }

        public void ResumeAfterRewind()
        {
            isRewinding = false;
        }
    }
}
