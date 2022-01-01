using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamagable
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth;
    [Space(10)]
    [SerializeField] private float regenFactor;
    [SerializeField] private float regenFrequency;
    [SerializeField] private float regenCooldown;
    private float currentHealth = 0f;
    private float regenTimer = 0f;
    private float regenCooldownTimer = 0f;

    [Header("Void Settings")]
    [SerializeField] private float killHeight;
    [SerializeField] private float killSpeed;

    public PlayerState State { get; private set; }

    public delegate void ChangePlayerState(PlayerState state);
    public event ChangePlayerState OnPlayerStateChanged;

    public delegate void DamageEntity(float damage);
    public event DamageEntity OnPlayerDamage;

    [Header("Assignables")]
    [SerializeField] private GameObject playerGraphics;

    public float MaxHealth { get { return maxHealth; } }
    public float CurrentHealth { get { return currentHealth; } }

    void Awake() => currentHealth = maxHealth;
    void Update()
    {
        if (transform.position.y < killHeight)
        {
            OnDamage(killSpeed * Time.deltaTime * 15f);
            return;
        }

        HandleRegen();
    }

    public void OnDamage(float damage)
    {
        if (State == PlayerState.Dead) return;
        if (damage > 0f) regenCooldownTimer = regenCooldown;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, MaxHealth);

        OnPlayerDamage?.Invoke(damage / maxHealth);

        if (currentHealth <= 0f) OnDeath();
    }

    public void OnDeath() 
    {
        if (State == PlayerState.Dead) return;
        if (playerGraphics != null) playerGraphics.SetActive(false);

        SetState(PlayerState.Dead);
    }

    private void SetState(PlayerState newState)
    {
        if (State == newState) return;

        State = newState;
        OnPlayerStateChanged?.Invoke(newState);
    }

    private void HandleRegen()
    {
        regenCooldownTimer -= Time.deltaTime;

        if (regenCooldownTimer > 0f)
        {
            regenCooldownTimer -= Time.deltaTime;
            return;
        }

        float curveFactor = 1f + (currentHealth / maxHealth);
        regenTimer += Time.deltaTime * regenFrequency * (curveFactor * curveFactor);

        if (regenTimer > 1f)
        {
            regenTimer = 0f;
            OnDamage(-regenFactor);
        }
    }
}

public enum PlayerState
{
    Alive,
    Dead,
}
