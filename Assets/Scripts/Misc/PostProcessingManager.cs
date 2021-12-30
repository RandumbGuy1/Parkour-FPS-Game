using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcessingManager : MonoBehaviour
{
    [Header("Depth Settings")]
    [SerializeField] private float focusSmoothTime;
    [SerializeField] private float playerDeathFocusDistance;

    [Header("Chromatic Settings")]
    [SerializeField] private float aberrationSmoothTime;
    [SerializeField] private float playerDeathAberration;

    [Header("Assignables")]
    [SerializeField] private ScriptManager s;
    public static PostProcessingManager Instance { get; private set; }

    private Volume volume;
    private DepthOfField depthProfile;
    private ChromaticAberration chromaProfile;

    void Awake()
    {
        Instance = this;

        volume = GetComponent<Volume>();
        volume.profile.TryGet(out depthProfile);
        volume.profile.TryGet(out chromaProfile);
    }

    void OnEnable() => s.PlayerHealth.OnPlayerStateChanged += OnPlayerStateChanged;
    void OnDisable() => s.PlayerHealth.OnPlayerStateChanged -= OnPlayerStateChanged;

    public void OnPlayerStateChanged(PlayerState newState)
    {
        if (newState != PlayerState.Dead) return;

        StartCoroutine(PingPongProfileValue(aberrationSmoothTime, playerDeathAberration, () => chromaProfile.intensity.value, result => chromaProfile.intensity.value = result));
        StartCoroutine(PingPongProfileValue(focusSmoothTime, playerDeathFocusDistance, () => depthProfile.focusDistance.value, result => depthProfile.focusDistance.value = result));
    }

    private IEnumerator PingPongProfileValue(float smoothing, float intensity, Func<float> currentVariable, Action<float> variableResult)
    {
        Vector2 vel = Vector2.zero;
        float startValue = currentVariable();

        while (intensity != startValue || currentVariable() != startValue)
        {
            intensity = Mathf.SmoothDamp(intensity, startValue, ref vel.x, smoothing * 2f);
            variableResult(Mathf.SmoothDamp(currentVariable(), intensity, ref vel.y, smoothing));
            
            if (Math.Abs(startValue - intensity) < 0.01f && Mathf.Abs(startValue - currentVariable()) < 0.01f)
            {
                intensity = startValue;
                variableResult(startValue);

                break;
            }

            yield return null;
        }
    }

    private IEnumerator SmoothProfileValue(float smoothing, float intensity, float startValue, Action<float> variableResult)
    {
        float elapsed = 0;

        while (elapsed < smoothing)
        {
            variableResult(Mathf.Lerp(startValue, intensity, Mathf.SmoothStep(0, 1, elapsed / smoothing)));
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}
