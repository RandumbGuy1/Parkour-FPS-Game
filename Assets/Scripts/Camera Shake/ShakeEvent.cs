﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShakeEvent
{
    private ShakeData shakeData;

    private float timeRemaining; 
    private float trama = 0f;

    private Vector3 noise = Vector3.zero;
    private Vector3 noiseOffset = Vector3.zero;

    private Vector3 desiredDir = Vector3.zero;
    private Vector3 targetDir = Vector3.zero;
    private Vector3 smoothDir = Vector3.zero;

    public Vector3 Displacement { get; private set; } = Vector3.zero;
    public bool Finished { get { return timeRemaining <= 0f; } }

    public ShakeEvent(ShakeData shakeData, Vector3 initialKickback)
    {
        this.shakeData = ScriptableObject.CreateInstance<ShakeData>();
        this.shakeData.Initialize(shakeData);

        timeRemaining = this.shakeData.Duration;

        if (shakeData.Type == ShakeData.ShakeType.KickBack)
        {
            desiredDir = initialKickback * this.shakeData.Magnitude;

            if (shakeData.Frequency > 0)
            {
                Vector3 randomDir = Random.insideUnitSphere;
                while (!InBounds(Vector3.Dot(-targetDir, randomDir), 0.5f, -0.5f)) randomDir = Random.insideUnitSphere;

                targetDir = (randomDir * 2.8f - desiredDir).normalized * shakeData.Magnitude;
            }
            else targetDir = Vector3.zero;
        }

        noiseOffset.x = Random.Range(0f, 32f);
        noiseOffset.y = Random.Range(0f, 32f);
        noiseOffset.z = Random.Range(0f, 32f);
    }

    bool InBounds(float amount, float max, float min) => amount >= min && amount <= max;

    public void UpdateShake()
    {
        timeRemaining -= Time.deltaTime;

        switch (shakeData.Type)
        {
            case ShakeData.ShakeType.KickBack:
                {
                    desiredDir = Vector3.Lerp(desiredDir, targetDir, shakeData.SmoothSpeed * 0.4f * Time.smoothDeltaTime);
                    smoothDir = Vector3.Slerp(smoothDir, desiredDir, shakeData.SmoothSpeed * Time.smoothDeltaTime);

                    Displacement = smoothDir * trama;

                    if (shakeData.Frequency <= 0) break;
                    if ((targetDir - desiredDir).sqrMagnitude < (shakeData.Magnitude * shakeData.Frequency * 0.05f) * (shakeData.Magnitude * shakeData.Frequency * 0.05f))
                    {
                        Vector3 randomDir = Random.insideUnitSphere;
                        while (!InBounds(Vector3.Dot(-targetDir, randomDir), 0.5f, -0.5f)) randomDir = Random.insideUnitSphere;

                        targetDir = (randomDir * 2.8f - targetDir).normalized * shakeData.Magnitude;
                    }

                    break;
                }

            case ShakeData.ShakeType.Perlin:
                {
                    float offsetDelta = Time.deltaTime * shakeData.Frequency;

                    noiseOffset += Vector3.one * offsetDelta;

                    noise.x = Mathf.PerlinNoise(noiseOffset.x, 1f);
                    noise.y = Mathf.PerlinNoise(noiseOffset.y, 2f);
                    noise.z = Mathf.PerlinNoise(noiseOffset.z, 3f);

                    noise -= Vector3.one * 0.5f;
                    noise *= shakeData.Magnitude;
                    noise *= trama;

                    Displacement = Vector3.Slerp(Displacement, noise, shakeData.SmoothSpeed * offsetDelta * 15f);
                    break;
                }
        }

        float agePercent = 1f - (timeRemaining / shakeData.Duration);
        trama = shakeData.BlendOverLifetime.Evaluate(agePercent);
        trama = Mathf.Clamp(trama, 0f, 1f);
    }
}
