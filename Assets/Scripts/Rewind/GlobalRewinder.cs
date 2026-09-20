using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonRewind.Rewind
{
    public class GlobalRewinder : MonoBehaviour
    {
        public void BeginRewindAll()
        {
            foreach (RewindableObject rewindable in FindObjectsByType<RewindableObject>(FindObjectsSortMode.None))
            {
                rewindable.BeginRewind();
            }
        }

        public void StopRewindAll()
        {
            foreach (RewindableObject rewindable in FindObjectsByType<RewindableObject>(FindObjectsSortMode.None))
            {
                rewindable.EndRewind();
            }
        }

        private void OnUseAbility(InputValue value)
        {
            if (value.isPressed)
            {
                BeginRewindAll();
            }
            else
            {
                StopRewindAll();
            }
        }
    }
}
