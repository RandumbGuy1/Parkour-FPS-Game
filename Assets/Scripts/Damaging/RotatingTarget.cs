using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatingTarget : MonoBehaviour, IDamagable
{
    [Header("Target Settings")]
    [SerializeField] private float maxHealth;
    [Space(10)]
    [SerializeField] private Vector3 rotation;
    [SerializeField] private float idleWaitTime;
    [SerializeField] private float rotateSmoothTime;
    private float currentHealth = 0f;
    private bool targetActive = true;

    [Header("Float Settings")]
    [SerializeField] private float floatWaveAmplitude;
    [SerializeField] private float floatWaveFrequency;

    [Header("Damage Settings")]
    [SerializeField] private Material targetMaterial;
    [SerializeField] private Color targetDamageColor;
    [SerializeField] private float normalColorSmoothTime;
    [Space(10)]
    [SerializeField] private float targetNormalIntensity;
    [SerializeField] private float targetDamageIntensity;
    [SerializeField] private float normalIntensitySmoothTime;
    [Space(10)]

    private float currentIntensity = 0f;
    private float desiredIntensity = 0f;

    private Color targetColor;

    public float MaxHealth { get { return maxHealth; } }
    public float CurrentHealth { get { return currentHealth; } }

    void Awake() => currentHealth = maxHealth;
    void OnEnable() => StartCoroutine(SineFloating(transform.position));

    public void OnDamage(float damage, PlayerManager player = null) 
    {
        if (!targetActive) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, Mathf.Infinity);

        EvaluateHitMarker(player);

        desiredIntensity = targetDamageIntensity;
        targetColor = targetDamageColor;

        if (currentHealth <= 0)
        {
            StartCoroutine(TargetRotateReset());
            return;
        }
    }

    private void EvaluateHitMarker(PlayerManager player) => player?.WeaponControls.HitMarker.Flash(transform.position, currentHealth <= 0);

    private IEnumerator TargetRotateReset()
    {
        targetActive = false;
        float elapsed = 0f;

        while (elapsed < rotateSmoothTime * 0.4f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(rotation), elapsed / rotateSmoothTime);
            elapsed += Time.deltaTime * 2f;

            yield return null;
        }

        yield return new WaitForSeconds(idleWaitTime);
        elapsed = 0f;

        while (elapsed < rotateSmoothTime * 0.2f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(Vector3.zero), elapsed / rotateSmoothTime);
            elapsed += Time.deltaTime * 2f;

            yield return null;
        }

        targetActive = true;
        currentHealth = maxHealth;
    }

    private IEnumerator SineFloating(Vector3 originalPos)
    {
        float scroller = 0f;

        while (true)
        {
            Vector3 finalPos = originalPos + floatWaveAmplitude * Mathf.Sin(scroller * floatWaveFrequency) * Vector3.up;
            transform.position = finalPos;

            scroller += Time.deltaTime;

            desiredIntensity = Mathf.Lerp(desiredIntensity, targetNormalIntensity, normalIntensitySmoothTime * 0.3f * Time.deltaTime);
            currentIntensity = Mathf.Lerp(currentIntensity, desiredIntensity, normalIntensitySmoothTime * 2f * Time.deltaTime);

            targetColor = Color.Lerp(targetColor, Color.white, normalColorSmoothTime * 0.05f * Time.deltaTime);
            targetMaterial.SetColor("_EmissionColor", Color.Lerp(targetMaterial.color, targetColor * currentIntensity, normalColorSmoothTime * Time.deltaTime * 2f));
            targetMaterial.SetColor("_Color", Color.Lerp(targetMaterial.color, targetColor * currentIntensity, normalColorSmoothTime * Time.deltaTime * 2f));

            yield return null;
        }
    }

    public void OnDeath() { }
}
