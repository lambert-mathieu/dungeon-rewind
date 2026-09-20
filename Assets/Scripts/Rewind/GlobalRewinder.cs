using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonRewind.Rewind
{
    public class GlobalRewinder : MonoBehaviour
    {
        public static GlobalRewinder Instance { get; private set; }

        public static event Action<bool> RewindStateChanged;

        [Header("Rewind Fuel")]
        [SerializeField] private float maxRewindTime = 5f;
        [SerializeField] private float rewindRegenSpeed = 1f;

        public float MaxRewindTime => maxRewindTime;
        public float CurrentRewindTime { get; private set; }

        private bool isRewinding = false;

        private void Awake()
        {
            Instance = this;
            CurrentRewindTime = maxRewindTime;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void BeginRewindAll()
        {
            if (CurrentRewindTime <= 0f) return;

            isRewinding = true;
            RewindStateChanged?.Invoke(true);

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

            if (isRewinding)
            {
                isRewinding = false;
                RewindStateChanged?.Invoke(false);
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

        private void Update()
        {
            if (isRewinding)
            {
                CurrentRewindTime = Mathf.Max(CurrentRewindTime - Time.deltaTime, 0f);

                if (CurrentRewindTime <= 0f)
                {
                    StopRewindAll();
                }
            }
            else
            {
                CurrentRewindTime = Mathf.Clamp(CurrentRewindTime + (Time.deltaTime * rewindRegenSpeed), 0f, maxRewindTime);
            }
        }
    }
}