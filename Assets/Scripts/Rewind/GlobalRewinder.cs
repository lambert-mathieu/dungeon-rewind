using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonRewind.Rewind
{
    public class GlobalRewinder : MonoBehaviour
    {
        public static event Action<bool> RewindStateChanged;

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
            bool isPressed = value.isPressed;
            if (isPressed)
            {
                BeginRewindAll();
            }
            else
            {
                StopRewindAll();
            }
            RewindStateChanged?.Invoke(isPressed);
        }
    }
}
