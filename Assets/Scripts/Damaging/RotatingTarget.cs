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
    [SerializeField] private float backToNormalColorSmoothTime;
    private Color targetColor;

    public float MaxHealth { get { return maxHealth; } }
    public float CurrentHealth { get { return currentHealth; } }

    void Awake()
    {
        currentHealth = maxHealth;
    }

    void OnEnable()
    {
        StartCoroutine(SineFloating(transform.position));
    }

    public void OnDamage(float damage) 
    {
        if (!targetActive) return;
        if (currentHealth <= 0)
        {
            StartCoroutine(TargetRotateReset());
            return;
        }

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, Mathf.Infinity);

        targetColor = targetDamageColor;
    }

    private IEnumerator TargetRotateReset()
    {
        targetActive = false;
        currentHealth = maxHealth;

        float elapsed = 0f;

        while (elapsed < rotateSmoothTime)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(rotation), elapsed / rotateSmoothTime);
            elapsed += Time.deltaTime * 2f;

            yield return null;
        }

        yield return new WaitForSeconds(idleWaitTime);
        elapsed = 0f;

        while (elapsed < rotateSmoothTime)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(Vector3.zero), elapsed / rotateSmoothTime);
            elapsed += Time.deltaTime * 2f;

            yield return null;
        }

        targetActive = true;
    }

    private IEnumerator SineFloating(Vector3 originalPos)
    {
        float scroller = 0f;

        while (true)
        {
            Vector3 finalPos = originalPos + floatWaveAmplitude * Mathf.Sin(scroller * floatWaveFrequency) * Vector3.up;
            transform.position = finalPos;

            scroller += Time.deltaTime;

            targetColor = Color.Lerp(targetColor, Color.grey, backToNormalColorSmoothTime * 0.5f * Time.deltaTime);
            targetMaterial.color = Color.Lerp(targetMaterial.color, targetColor, backToNormalColorSmoothTime * Time.deltaTime * 2f);

            yield return null;
        }
    }

    public void OnDeath() { }
}
