using UnityEngine;

namespace DungeonRewind.Enemy {
    public class EnemyLogic : MonoBehaviour {
        [SerializeField] private int maxHealth = 30;
        [SerializeField] private Transform enemyVisual;

        private int currentHealth;
        private Transform playerTransform;

        private void Awake() {
            currentHealth = maxHealth;

            if (Camera.main != null) {
                playerTransform = Camera.main.transform;
            }
        }

        private void LateUpdate() {
            if (playerTransform == null || enemyVisual == null) {
                return;
            }

            Vector3 direction = enemyVisual.position - playerTransform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f) {
                return;
            }

            enemyVisual.rotation = Quaternion.LookRotation(direction);
        }

        public void TakeDamage(int amount) {
            currentHealth -= amount;
            if (currentHealth <= 0) {
                Destroy(gameObject);
            }
        }
    }
}
