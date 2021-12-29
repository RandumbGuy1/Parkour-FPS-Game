using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamagable
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth;
    private float currentHealth = 0f;

    public PlayerState State { get; private set; }

    public delegate void ChangePlayerState(PlayerState state);
    public event ChangePlayerState OnPlayerStateChanged;

    [Header("Assignables")]
    [SerializeField] private GameObject playerGraphics;
    [SerializeField] private GameObject playerRagdoll;

    public float MaxHealth { get { return maxHealth; } }
    public float CurrentHealth { get { return currentHealth; } }

    void Awake() => currentHealth = maxHealth;

    public void OnDamage(float damage)
    {
        if (State == PlayerState.Dead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, Mathf.Infinity);

        if (currentHealth <= 0f) OnDeath();
    }

    public void OnDeath() 
    {
        SetState(PlayerState.Dead);

        if (playerGraphics != null & playerRagdoll != null)
        {
            playerGraphics.SetActive(false);
            playerRagdoll.SetActive(true);
        }
    }

    private void SetState(PlayerState newState)
    {
        if (State == newState) return;

        State = newState;
        OnPlayerStateChanged?.Invoke(newState);
    }
}

public enum PlayerState
{
    Alive,
    Dead,
}
