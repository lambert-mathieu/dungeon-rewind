using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace DungeonRewind.Rewind {
    public class RewindDebugTrigger : MonoBehaviour {
        [SerializeField] private RewindableObject target;
        [SerializeField] private Key rewindKey = Key.R;

        private void Awake() {
            if (target == null) {
                target = GetComponent<RewindableObject>();
            }
        }

        private void Update() {
            if (target == null || Keyboard.current == null) {
                return;
            }

            KeyControl key = Keyboard.current[rewindKey];

            if (key.wasPressedThisFrame && !target.IsRewinding) {
                target.BeginRewind();
            } else if (key.wasReleasedThisFrame && target.IsRewinding) {
                target.EndRewind();
            }
        }

        [ContextMenu("Begin Rewind")]
        private void BeginRewindFromInspector() {
            if (target != null && !target.IsRewinding) {
                target.BeginRewind();
            }
        }

        [ContextMenu("End Rewind")]
        private void EndRewindFromInspector() {
            if (target != null && target.IsRewinding) {
                target.EndRewind();
            }
        }
    }
}
