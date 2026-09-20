using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonRewind.Player {
    public class PlayerAttack : MonoBehaviour {
        private const int punchFrameCount = 3;
        private const float punchFrameDuration = 0.15f;
        private const float extraCooldownDuration = 0f;
        private const float attackBufferWindowDuration = 0.1f;
        private const float punchAnimationDuration = punchFrameCount * punchFrameDuration;
        private const float totalCooldownDuration = punchAnimationDuration + extraCooldownDuration;

        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform rightArm;

        private GameObject leftArmDefault;
        private GameObject rightArmDefault;
        private GameObject[] leftPunchStages;
        private GameObject[] rightPunchStages;

        private bool isRightArmNext = true;
        private bool isAttackingRightArm;
        private bool isAttackOnCooldown;
        private int currentPunchFrameIndex = -1;
        private float attackElapsedTime;
        private bool attackBuffered;

        private void Awake() {
            leftArmDefault = leftArm.Find("ArmDefault").gameObject;
            rightArmDefault = rightArm.Find("ArmDefault").gameObject;
            leftPunchStages = ResolvePunchStages(leftArm);
            rightPunchStages = ResolvePunchStages(rightArm);
        }

        private void OnEnable() {
            ResetArmsToDefaultPose();
        }

        private void OnDisable() {
            ResetArmsToDefaultPose();
        }

        private void Update() {
            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f) {
                return;
            }

            UpdateActiveAttack(deltaTime);
        }

        public void OnAttack(InputValue value) {
            if (!value.isPressed) {
                return;
            }

            HandleAttackInput();
        }

        private void HandleAttackInput() {
            if (Cursor.lockState != CursorLockMode.Locked) {
                return;
            }

            if (isAttackOnCooldown) {
                if (totalCooldownDuration - attackElapsedTime <= attackBufferWindowDuration) {
                    attackBuffered = true;
                }
                return;
            }

            TriggerAttack();
        }

        private void TriggerAttack() {
            isAttackingRightArm = isRightArmNext;
            isRightArmNext = !isRightArmNext;

            GetArmDefault(isAttackingRightArm).SetActive(false);
            GetPunchStages(isAttackingRightArm)[0].SetActive(true);

            currentPunchFrameIndex = 0;
            attackElapsedTime = 0f;
            isAttackOnCooldown = true;
        }

        private void UpdateActiveAttack(float deltaTime) {
            if (!isAttackOnCooldown) {
                return;
            }

            attackElapsedTime += deltaTime;

            if (currentPunchFrameIndex != -1) {
                UpdatePunchFrame();
            }

            if (attackElapsedTime < totalCooldownDuration) {
                return;
            }

            isAttackOnCooldown = false;
            if (attackBuffered) {
                attackBuffered = false;
                TriggerAttack();
            }
        }

        private void UpdatePunchFrame() {
            if (attackElapsedTime >= punchAnimationDuration) {
                GetPunchStages(isAttackingRightArm)[currentPunchFrameIndex].SetActive(false);
                GetArmDefault(isAttackingRightArm).SetActive(true);
                currentPunchFrameIndex = -1;
                return;
            }

            int targetFrameIndex = Mathf.Min(punchFrameCount - 1, (int) (attackElapsedTime / punchFrameDuration));
            if (targetFrameIndex == currentPunchFrameIndex) {
                return;
            }

            GetPunchStages(isAttackingRightArm)[currentPunchFrameIndex].SetActive(false);
            currentPunchFrameIndex = targetFrameIndex;
            GetPunchStages(isAttackingRightArm)[currentPunchFrameIndex].SetActive(true);
        }

        private void ResetArmsToDefaultPose() {
            ResetArmToDefaultPose(leftArmDefault, leftPunchStages);
            ResetArmToDefaultPose(rightArmDefault, rightPunchStages);
            isAttackOnCooldown = false;
            currentPunchFrameIndex = -1;
            attackElapsedTime = 0f;
            attackBuffered = false;
        }

        private static void ResetArmToDefaultPose(GameObject armDefault, GameObject[] punchStages) {
            armDefault.SetActive(true);
            for (int i = 0; i < punchStages.Length; i++) {
                punchStages[i].SetActive(false);
            }
        }

        private GameObject[] GetPunchStages(bool isRightArm) {
            return isRightArm ? rightPunchStages : leftPunchStages;
        }

        private GameObject GetArmDefault(bool isRightArm) {
            return isRightArm ? rightArmDefault : leftArmDefault;
        }

        private static GameObject[] ResolvePunchStages(Transform armRoot) {
            return new GameObject[] {
                armRoot.Find("ArmPunch1").gameObject,
                armRoot.Find("ArmPunch2").gameObject,
                armRoot.Find("ArmPunch3").gameObject
            };
        }
    }
}
