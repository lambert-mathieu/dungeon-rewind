using System.Collections;
using DungeonRewind.Combat;
using DungeonRewind.Rewind;
using UnityEngine;

namespace DungeonRewind.Enemy {
    [RequireComponent(typeof(Rigidbody))]
    public abstract class EnemyLogic : MonoBehaviour, IDamageable, IRewindSuspendable {
        protected enum EnemyState { Idle, Reposition, PrepareAttack, Attack }

        [SerializeField] private Transform enemyVisual;
        [SerializeField] private GameObject idleVisual;
        [SerializeField] private GameObject move1Visual;
        [SerializeField] private GameObject move2Visual;
        [SerializeField] private GameObject prepareAttackVisual;
        [SerializeField] private GameObject attackVisual;
        private Color damageFlashColorRewind = new Color(0.557f, 0.239f, 0.745f);
        private Color damageFlashColorDefault = new Color(0.87f, 0.04f, 0.15f);
        private float damageFlashDuration = 0.1f;

        protected abstract int MaxHealth { get; }
        protected abstract float DesiredAttackDistance { get; }
        protected abstract float AttackDistanceTolerance { get; }
        protected abstract float AggroGetDistance { get; }
        protected abstract float AggroLoseDistance { get; }
        protected abstract float MinAttackCooldown { get; }
        protected abstract float MaxAttackCooldown { get; }
        protected abstract float MoveSpeed { get; }
        protected abstract float PrepareAttackDuration { get; }
        protected virtual float MoveFrameInterval => 0.3f;
        protected virtual float AttackPoseDuration => 0.5f;

        private int currentHealth;
        private Rigidbody rb;
        private SpriteRenderer[] spriteRenderers;
        private Color[] originalSpriteColors;
        private Coroutine damageFlashRoutine;
        private EnemyState currentState;
        private float stateTimer;
        private float timeSinceLastAttack;
        private bool isShowingMove1 = true;

        private bool isRewinding = false;

        private void Awake() {
            currentHealth = MaxHealth;
            TryGetComponent(out rb);
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            originalSpriteColors = new Color[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++) {
                originalSpriteColors[i] = spriteRenderers[i].color;
            }
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            EnterState(EnemyState.Idle);
        }

        private void Update() {
            if (isRewinding)
            {
                return;
            }
            timeSinceLastAttack += Time.deltaTime;

            if (PlayerGlobal.PlayerTransform == null) {
                if (currentState != EnemyState.Idle) {
                    EnterState(EnemyState.Idle);
                }
                return;
            }

            float distanceToPlayer = GetHorizontalDistanceToPlayer(transform.position);

            switch (currentState) {
                case EnemyState.Idle:
                    if (distanceToPlayer <= AggroGetDistance) {
                        EnterState(EnemyState.Reposition);
                    }
                    break;

                case EnemyState.Reposition:
                    if (distanceToPlayer > AggroLoseDistance) {
                        EnterState(EnemyState.Idle);
                        break;
                    }

                    bool isInAttackBand = Mathf.Abs(distanceToPlayer - DesiredAttackDistance) <= AttackDistanceTolerance;
                    if (isInAttackBand) {
                        SetActiveVisual(idleVisual);
                    } else {
                        UpdateMoveAnimation();
                    }

                    bool canAttack = timeSinceLastAttack >= MinAttackCooldown;
                    bool mustForceAttack = timeSinceLastAttack >= MaxAttackCooldown;
                    if (canAttack && (isInAttackBand || mustForceAttack)) {
                        EnterState(EnemyState.PrepareAttack);
                    }
                    break;

                case EnemyState.PrepareAttack:
                    if (distanceToPlayer > AggroLoseDistance) {
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
                        EnterState(distanceToPlayer > AggroLoseDistance ? EnemyState.Idle : EnemyState.Reposition);
                    }
                    break;
            }
        }

        private void FixedUpdate() {
            if (isRewinding)
            {
                return;
            }
            if (currentState != EnemyState.Reposition || PlayerGlobal.PlayerTransform == null) {
                ZeroHorizontalVelocity();
                return;
            }

            Vector3 playerPosition = GetFlattenedPlayerPosition(rb.position.y);
            float distanceToPlayer = Vector3.Distance(rb.position, playerPosition);
            float distanceError = distanceToPlayer - DesiredAttackDistance;
            if (Mathf.Abs(distanceError) <= AttackDistanceTolerance) {
                ZeroHorizontalVelocity();
                return;
            }

            Vector3 directionToPlayer = (playerPosition - rb.position).normalized;
            Vector3 moveDirection = distanceError > 0f ? directionToPlayer : -directionToPlayer;
            Vector3 horizontalVelocity = moveDirection * MoveSpeed;
            rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
        }

        private void ZeroHorizontalVelocity() {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
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

        private Vector3 GetFlattenedPlayerPosition(float atHeight) {
            Vector3 playerPosition = PlayerGlobal.PlayerTransform.position;
            playerPosition.y = atHeight;
            return playerPosition;
        }

        private float GetHorizontalDistanceToPlayer(Vector3 fromPosition) {
            return Vector3.Distance(fromPosition, GetFlattenedPlayerPosition(fromPosition.y));
        }

        private void EnterState(EnemyState newState) {
            currentState = newState;

            switch (newState) {
                case EnemyState.Idle:
                    timeSinceLastAttack = 0f;
                    SetActiveVisual(idleVisual);
                    break;
                case EnemyState.Reposition:
                    isShowingMove1 = true;
                    stateTimer = MoveFrameInterval;
                    SetActiveVisual(move1Visual);
                    break;
                case EnemyState.PrepareAttack:
                    stateTimer = PrepareAttackDuration;
                    SetActiveVisual(prepareAttackVisual);
                    break;
                case EnemyState.Attack:
                    stateTimer = AttackPoseDuration;
                    timeSinceLastAttack = 0f;
                    SetActiveVisual(attackVisual);
                    PerformAttack();
                    break;
            }
        }

        private void UpdateMoveAnimation() {
            stateTimer -= Time.deltaTime;
            if (stateTimer > 0f) {
                return;
            }

            stateTimer = MoveFrameInterval;
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

        protected abstract void PerformAttack();

        public void TakeDamage(int amount, bool causedByRewindMagic) {
            currentHealth -= amount;

            Color damageColor = causedByRewindMagic ? damageFlashColorRewind : damageFlashColorDefault;
            if (currentHealth <= 0) {
                StartCoroutine(DieFlashRoutine(damageColor));
                return;
            }

            if (damageFlashRoutine != null) {
                StopCoroutine(damageFlashRoutine);
            }
            damageFlashRoutine = StartCoroutine(DamageFlashRoutine(damageColor));
        }

        private IEnumerator DamageFlashRoutine(Color damageColor) {
            SetSpriteColor(damageColor);
            yield return new WaitForSeconds(damageFlashDuration);
            RestoreSpriteColors();
            yield return new WaitForSeconds(damageFlashDuration);
            SetSpriteColor(damageColor);
            yield return new WaitForSeconds(damageFlashDuration);
            RestoreSpriteColors();
            damageFlashRoutine = null;
        }

        private IEnumerator DieFlashRoutine(Color damageColor)
        {
            SetSpriteColor(damageColor);
            yield return new WaitForSeconds(damageFlashDuration);
            Destroy(gameObject);
        }

        private void SetSpriteColor(Color color) {
            foreach (SpriteRenderer spriteRenderer in spriteRenderers) {
                spriteRenderer.color = color;
            }
        }

        private void RestoreSpriteColors() {
            for (int i = 0; i < spriteRenderers.Length; i++) {
                spriteRenderers[i].color = originalSpriteColors[i];
            }
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
