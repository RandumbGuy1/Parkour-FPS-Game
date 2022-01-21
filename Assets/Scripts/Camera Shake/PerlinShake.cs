using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerlinShake : IShakeEvent
{
    private readonly ShakeData shakeData;
    public ShakeData ShakeData { get { return shakeData; } }

    public Vector3 ShakeOffset { get; private set; }
    public bool Finished { get; }

    private float timeRemaining;
    private float trama = 0f;

    private Vector3 noise = Vector3.zero;
    private Vector3 noiseOffset = Vector3.zero;

    public PerlinShake(ShakeData shakeData)
    {
        this.shakeData = ScriptableObject.CreateInstance<ShakeData>();
        this.shakeData.Initialize(shakeData);

        timeRemaining = this.shakeData.Duration;

        noiseOffset.x = Random.Range(0f, 32f);
        noiseOffset.y = Random.Range(0f, 32f);
        noiseOffset.z = Random.Range(0f, 32f);
    }

    public void UpdateShake(float deltaTime)
    {
        float offsetDelta = deltaTime * shakeData.Frequency;
        timeRemaining -= deltaTime;

        noiseOffset += Vector3.one * offsetDelta;

        noise.x = Mathf.PerlinNoise(noiseOffset.x, 1f);
        noise.y = Mathf.PerlinNoise(noiseOffset.y, 2f);
        noise.z = Mathf.PerlinNoise(noiseOffset.z, 3f);

        noise -= Vector3.one * 0.5f;
        noise *= shakeData.Magnitude;
        noise *= trama;

        ShakeOffset = Vector3.Lerp(ShakeOffset, noise, shakeData.SmoothSpeed * deltaTime * 15f);

        float agePercent = 1f - (timeRemaining / shakeData.Duration);
        trama = shakeData.BlendOverLifetime.Evaluate(agePercent);
        trama = Mathf.Clamp01(trama);
    }
}
