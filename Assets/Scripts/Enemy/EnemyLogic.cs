using UnityEngine;

namespace DungeonRewind.Enemy {
    [RequireComponent(typeof(Rigidbody))]
    public class EnemyLogic : MonoBehaviour {
        private enum EnemyState { Idle, Move, PrepareAttack, Attack }

        [SerializeField] private int maxHealth = 30;
        [SerializeField] private Transform enemyVisual;
        [SerializeField] private GameObject idleVisual;
        [SerializeField] private GameObject move1Visual;
        [SerializeField] private GameObject move2Visual;
        [SerializeField] private GameObject prepareAttackVisual;
        [SerializeField] private GameObject attackVisual;

        [SerializeField] private float detectionRange = 10f;
        [SerializeField] private float attackRange = 6f;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float moveFrameInterval = 0.3f;
        [SerializeField] private float prepareAttackDuration = 0.5f;
        [SerializeField] private float attackPoseDuration = 0.5f;
        [SerializeField] private float fireballSpawnHeight = 1.5f;
        [SerializeField] private float fireballRadius = 0.25f;

        private int currentHealth;
        private Rigidbody rb;
        private EnemyState currentState;
        private float stateTimer;
        private bool isShowingMove1 = true;

        private void Awake() {
            currentHealth = maxHealth;
            TryGetComponent(out rb);
            rb.isKinematic = true;
            EnterState(EnemyState.Idle);
        }

        private void Update() {
            if (PlayerGlobal.PlayerTransform == null) {
                if (currentState != EnemyState.Idle) {
                    EnterState(EnemyState.Idle);
                }
                return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, PlayerGlobal.PlayerTransform.position);

            switch (currentState) {
                case EnemyState.Idle:
                    if (distanceToPlayer <= attackRange) {
                        EnterState(EnemyState.PrepareAttack);
                    } else if (distanceToPlayer <= detectionRange) {
                        EnterState(EnemyState.Move);
                    }
                    break;

                case EnemyState.Move:
                    if (distanceToPlayer <= attackRange) {
                        EnterState(EnemyState.PrepareAttack);
                    } else if (distanceToPlayer > detectionRange) {
                        EnterState(EnemyState.Idle);
                    } else {
                        UpdateMoveAnimation();
                    }
                    break;

                case EnemyState.PrepareAttack:
                    if (distanceToPlayer > detectionRange) {
                        EnterState(EnemyState.Idle);
                        break;
                    }

                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0f) {
                        EnterState(EnemyState.Attack);
                    }
                    break;

                case EnemyState.Attack:
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0f) {
                        EnterState(distanceToPlayer <= attackRange ? EnemyState.PrepareAttack : EnemyState.Idle);
                    }
                    break;
            }
        }

        private void FixedUpdate() {
            if (currentState != EnemyState.Move || PlayerGlobal.PlayerTransform == null) {
                return;
            }

            Vector3 targetPosition = PlayerGlobal.PlayerTransform.position;
            targetPosition.y = rb.position.y;

            Vector3 newPosition = Vector3.MoveTowards(rb.position, targetPosition, moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(newPosition);
        }

        private void LateUpdate() {
            if (PlayerGlobal.PlayerTransform == null || enemyVisual == null) {
                return;
            }

            Vector3 direction = enemyVisual.position - PlayerGlobal.PlayerTransform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f) {
                return;
            }

            enemyVisual.rotation = Quaternion.LookRotation(direction);
        }

        private void EnterState(EnemyState newState) {
            currentState = newState;

            switch (newState) {
                case EnemyState.Idle:
                    SetActiveVisual(idleVisual);
                    break;
                case EnemyState.Move:
                    isShowingMove1 = true;
                    stateTimer = moveFrameInterval;
                    SetActiveVisual(move1Visual);
                    break;
                case EnemyState.PrepareAttack:
                    stateTimer = prepareAttackDuration;
                    SetActiveVisual(prepareAttackVisual);
                    break;
                case EnemyState.Attack:
                    stateTimer = attackPoseDuration;
                    SetActiveVisual(attackVisual);
                    ThrowFireball();
                    break;
            }
        }

        private void UpdateMoveAnimation() {
            stateTimer -= Time.deltaTime;
            if (stateTimer > 0f) {
                return;
            }

            stateTimer = moveFrameInterval;
            isShowingMove1 = !isShowingMove1;
            SetActiveVisual(isShowingMove1 ? move1Visual : move2Visual);
        }

        private void SetActiveVisual(GameObject visualToShow) {
            idleVisual.SetActive(idleVisual == visualToShow);
            move1Visual.SetActive(move1Visual == visualToShow);
            move2Visual.SetActive(move2Visual == visualToShow);
            prepareAttackVisual.SetActive(prepareAttackVisual == visualToShow);
            attackVisual.SetActive(attackVisual == visualToShow);
        }

        private void ThrowFireball() {
            Vector3 spawnPosition = transform.position + Vector3.up * fireballSpawnHeight;
            Vector3 directionToPlayer = (PlayerGlobal.PlayerTransform.position - spawnPosition).normalized;

            GameObject fireball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fireball.name = "Fireball";
            fireball.transform.SetPositionAndRotation(spawnPosition, Quaternion.LookRotation(directionToPlayer));
            fireball.transform.localScale = Vector3.one * fireballRadius * 2f;
        }

        public void TakeDamage(int amount) {
            currentHealth -= amount;
            if (currentHealth <= 0) {
                Destroy(gameObject);
            }
        }
    }
}
