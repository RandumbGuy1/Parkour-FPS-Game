﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamagable
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth;
    [SerializeField] private bool invincible = false;
    [SerializeField] private bool flashMarker = true;
    [Space(10)]
    [SerializeField] private float regenFactor;
    [SerializeField] private float regenFrequency;
    [SerializeField] private float regenCooldown;
    private float currentHealth = 0f;
    private float regenTimer = 0f;
    private float regenCooldownTimer = 0f;

    [Header("Invincibility Settings")]
    [SerializeField] private float invincibilityTime;
    private float currentInvincibility = 0;

    [Header("Void Settings")]
    [SerializeField] private float killHeight;
    [SerializeField] private float killSpeed;

    public UnitState State { get; private set; }

    public delegate void ChangePlayerState(UnitState state);
    public event ChangePlayerState OnPlayerStateChanged;

    public delegate void DamageEntity(float damage);
    public event DamageEntity OnPlayerDamage;

    [Header("Assignables")]
    [SerializeField] private GameObject playerGraphics;
    [SerializeField] private string deathEffect;

    public float MaxHealth { get { return maxHealth; } }
    public float CurrentHealth { get { return currentHealth; } }

    void Awake() => currentHealth = maxHealth;
    void Update()
    {
        if (transform.position.y < killHeight)
        {
            currentInvincibility = 0;
            OnDamage(killSpeed * Time.deltaTime * 15f);
            return;
        }

        HandleRegen();
        HandleInvincibility();
    }

    public void OnDamage(float damage, PlayerManager player = null)
    {
        if (State == UnitState.Dead) return;

        if (damage > 0f)
        {
            if (currentInvincibility > 0) return;

            regenCooldownTimer = regenCooldown;
            currentInvincibility = invincibilityTime;
        }

        if (!invincible)
        {
            currentHealth -= damage;
            currentHealth = Mathf.Clamp(currentHealth, 0, MaxHealth);

            if (flashMarker) EvaluateHitMarker(player);

            OnPlayerDamage?.Invoke(damage / maxHealth);
        }

        if (currentHealth <= 0f) OnDeath();
    }

    private void EvaluateHitMarker(PlayerManager player) => player?.WeaponControls.HitMarker.Flash(transform.position, currentHealth <= 0);

    public void OnDeath() 
    {
        if (State == UnitState.Dead) return;
        if (playerGraphics != null) playerGraphics.SetActive(false);
        ObjectPooler.Instance.Spawn(deathEffect, transform.position, Quaternion.identity);

        SetState(UnitState.Dead);
    }

    public void SetState(UnitState newState)
    {
        if (State == newState) return;

        State = newState;
        OnPlayerStateChanged?.Invoke(newState);

        if (State == UnitState.Alive) currentHealth = maxHealth;
    }

    private void HandleInvincibility()
    {
        if (currentInvincibility <= 0)
        {
            currentInvincibility = 0f;
            return;
        }

        currentInvincibility -= Time.deltaTime;
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

public enum UnitState
{
    Alive,
    Dead,
}
