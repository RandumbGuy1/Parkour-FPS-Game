using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShakeEvent
{
    private ShakeData shakeData;

    private float timeRemaining; 
    private float trama = 0f;

    private Vector3 noise = Vector3.zero;
    private Vector3 noiseOffset = Vector3.zero;

    private Vector3 targetDir = Vector3.zero;
    private Vector3 smoothDir = Vector3.zero;
    private Vector3 vel = Vector3.zero;

    public Vector3 Displacement { get; private set; } = Vector3.zero;
    public bool Finished { get { return timeRemaining <= 0f; } }

    public ShakeEvent(ShakeData shakeData, Vector3 initialKickback)
    {
        this.shakeData = shakeData;
        timeRemaining = this.shakeData.duration;

        targetDir = initialKickback;

        noiseOffset.x = Random.Range(0f, 32f);
        noiseOffset.y = Random.Range(0f, 32f);
        noiseOffset.z = Random.Range(0f, 32f);
    }

    public void UpdateShake()
    {
        timeRemaining -= Time.deltaTime;

        switch (shakeData.type)
        {
            case ShakeData.ShakeType.KickBack:
                {
                    if ((targetDir - smoothDir).sqrMagnitude < (shakeData.frequency * 0.1f)) targetDir = (-targetDir + Random.insideUnitSphere * 0.3f).normalized;
                    smoothDir = Vector3.SmoothDamp(smoothDir, targetDir, ref vel, shakeData.smoothness);

                    Displacement = (smoothDir * shakeData.magnitude) * trama;
                    break;
                }

            case ShakeData.ShakeType.Perlin:
                {
                    float offsetDelta = Time.deltaTime * shakeData.frequency;

                    noiseOffset += Vector3.one * offsetDelta;

                    noise.x = Mathf.PerlinNoise(noiseOffset.x, 1f);
                    noise.y = Mathf.PerlinNoise(noiseOffset.y, 2f);
                    noise.z = Mathf.PerlinNoise(noiseOffset.z, 3f);

                    noise -= Vector3.one * 0.5f;
                    noise *= shakeData.magnitude;
                    noise *= trama;

                    Displacement = Vector3.SmoothDamp(Displacement, noise, ref vel, shakeData.smoothness);
                    break;
                }
        }

        float agePercent = 1f - (timeRemaining / shakeData.duration);
        trama = shakeData.blendOverLifetime.Evaluate(agePercent);
        trama = Mathf.Clamp(trama, 0f, 1f);
    }
}
