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
    private float desiredFocusDistance;
    private float startFocusDistance;
    private bool updateDepth = true;

    [Header("Chromatic Settings")]
    [SerializeField] private float aberrationSmoothTime;
    [SerializeField] private float playerDeathAberration;
    private float desiredAberration;
    private float startAberration;
    private bool updateAberration = true;

    [Header("Vignette Settings")]
    [SerializeField] private float vignetteSmoothTime;
    private float desiredVignette;
    private float startVignette;
    private bool updateVignette = true;

    [Header("Assignables")]
    [SerializeField] private ScriptManager s;
    public static PostProcessingManager Instance { get; private set; }

    private Volume volume;
    private DepthOfField depthProfile;
    private ChromaticAberration chromaProfile;
    private Vignette vignetteProfile;

    void Awake()
    {
        Instance = this;

        volume = GetComponent<Volume>();
        updateDepth = volume.profile.TryGet(out depthProfile);
        updateAberration = volume.profile.TryGet(out chromaProfile);
        updateVignette = volume.profile.TryGet(out vignetteProfile);

        if (updateVignette) desiredVignette = vignetteProfile.intensity.value;
        if (updateAberration) desiredAberration = chromaProfile.intensity.value;
        if (updateDepth) desiredFocusDistance = depthProfile.focusDistance.value;

        startFocusDistance = desiredFocusDistance;
        startAberration = desiredAberration;
        startVignette = desiredVignette;
    }

    void OnEnable()
    {
        StartCoroutine(UpdatePostProcessing());

        s.PlayerHealth.OnPlayerStateChanged += OnPlayerStateChanged;
        s.PlayerHealth.OnPlayerDamage += OnPlayerDamage;
    }
        
    void OnDisable()
    {
        s.PlayerHealth.OnPlayerStateChanged -= OnPlayerStateChanged;
        s.PlayerHealth.OnPlayerDamage -= OnPlayerDamage;
    }

    public void OnPlayerDamage(float damage)
    {
        desiredVignette += damage * 0.7f;
        desiredVignette = Mathf.Clamp(desiredVignette, startVignette, Mathf.Infinity);

        desiredFocusDistance -= damage;
        desiredFocusDistance = Mathf.Clamp(desiredFocusDistance, 0f, startFocusDistance);
    }

    public void OnPlayerStateChanged(UnitState newState)
    {
        if (newState != UnitState.Dead) return;

        desiredFocusDistance = playerDeathFocusDistance;
        desiredAberration = playerDeathAberration;
        desiredVignette = 0f;

        vignetteSmoothTime = 0.15f;
    }

    public void ResetDeathValues()
    {
        desiredFocusDistance = startFocusDistance;
        desiredAberration = startAberration;
    }

    private IEnumerator UpdatePostProcessing()
    {
        while (true)
        {
            if (updateVignette) SmoothlyChangeValues(result => vignetteProfile.intensity.value = result, () => vignetteProfile.intensity.value, vignetteSmoothTime, desiredVignette, 0f);
            if (updateDepth) SmoothlyChangeValues(result => depthProfile.focusDistance.value = result, () => depthProfile.focusDistance.value, focusSmoothTime, desiredFocusDistance, 0f);
            if (updateAberration) SmoothlyChangeValues(result => chromaProfile.intensity.value = result, () => chromaProfile.intensity.value, aberrationSmoothTime, desiredAberration, 0f);

            yield return null;
        }
    }

    private void SmoothlyChangeValues(Action<float> SetValue, Func<float> Value, float smoothing, float intensity, float velocity)
    {
        if (Value() == intensity) return;

        SetValue(Mathf.SmoothDamp(Value(), intensity, ref velocity, smoothing));

        if (Math.Abs(intensity - Value()) < 0.01f) SetValue(intensity);
    }

    /*
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
    */
}
