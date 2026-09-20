using UnityEngine;

namespace DungeonRewind.Enemy {
    public class EnemyHealth : MonoBehaviour {
        [SerializeField] private int maxHealth = 30;

        private int currentHealth;

        private void Awake() {
            currentHealth = maxHealth;
        }

        public void TakeDamage(int amount) {
            currentHealth -= amount;
            if (currentHealth <= 0) {
                Destroy(gameObject);
            }
        }
    }
}
