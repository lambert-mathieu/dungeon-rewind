using System;
using DungeonRewind.Combat;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerState : IDamageable {
    public int MaxHealth { get; private set; }
    public int CurrentHealth { get; private set; }

    public PlayerState(int maxHealth) {
        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(int amount, bool causedByRewindMagic) {
        CurrentHealth = Mathf.Clamp(CurrentHealth - amount, 0, MaxHealth);
    }
}

public class PlayerGlobal : MonoBehaviour, IDamageable {
    public static PlayerGlobal Instance { get; private set; }
    public static Transform PlayerTransform { get; private set; }
    public static GameObject PlayerObject { get; private set; }
    public static PlayerState State { get; private set; } = new PlayerState(200);
    public static event Action PlayerDamaged;

    private const int maxHealth = 50;
    private const string mainMenuSceneName = "MainMenu";

    private void OnEnable() {
        Instance = this;
        PlayerTransform = transform;
        PlayerObject = gameObject;
        State = new PlayerState(maxHealth);
    }

    private void OnDisable() {
        if (Instance == this) {
            Instance = null;
        }

        if (PlayerTransform == transform) {
            PlayerTransform = null;
        }

        if (PlayerObject == gameObject) {
            PlayerObject = null;
        }
    }

    public void TakeDamage(int amount, bool causedByRewindMagic) {
        State.TakeDamage(amount, causedByRewindMagic);
        PlayerDamaged?.Invoke();

        if (State.CurrentHealth <= 0) {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
