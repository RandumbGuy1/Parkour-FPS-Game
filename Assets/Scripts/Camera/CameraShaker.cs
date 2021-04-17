using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    [Header("Shake Settings")]
    public float maxMagnitude;

    public AnimationCurve blendOverLifetime = new AnimationCurve(

      new Keyframe(0.0f, 0.0f, Mathf.Deg2Rad * 0.0f, Mathf.Deg2Rad * 720.0f),
      new Keyframe(0.2f, 1.0f),
      new Keyframe(1.0f, 0.0f));

    public Vector3 offset { get; private set; }

    private bool isShaking = false;
    private float lastMagnitude = 0f;
    private float lastDuration = 0f;

    public void ShakeOnce(float magnitude, float frequency, float duration)
    {
        if (isShaking)
        {
            magnitude += (lastMagnitude * 0.9f);
            duration += (lastDuration * 0.6f);
            magnitude = Mathf.Clamp(magnitude, 0, maxMagnitude);

            StopAllCoroutines();
            StartCoroutine(Shake(magnitude, frequency, duration));
        }
        else if (!isShaking) StartCoroutine(Shake(magnitude, frequency, duration));

        isShaking = true;
    }

    IEnumerator Shake(float magitude, float frequency, float duration)
    {
        float vel = 0f;

        float timeRemaining = duration;
        float trama = 0f;
        float agePercent;

        float scroller = 0f;
        float seed = Random.value;
        Vector3 noise = Vector3.zero;

        lastMagnitude = magitude;

        while (timeRemaining > 0f)
        {
            timeRemaining -= Time.deltaTime;
            scroller += Time.deltaTime * frequency;
            lastDuration = timeRemaining;

            noise.x = Mathf.PerlinNoise(seed, scroller);
            noise.y = Mathf.PerlinNoise(seed + 1f, scroller);
            noise.z = Mathf.PerlinNoise(seed + 2f, scroller);

            noise -= Vector3.one * 0.5f;
            noise *= magitude;

            agePercent = 1f - (timeRemaining / duration);
            trama = blendOverLifetime.Evaluate(agePercent);
            trama = Mathf.Clamp(trama, 0f, 1f);
            noise *= trama;

            offset = noise;

            yield return null;
        }

        offset = Vector3.zero;
        isShaking = false;
    }
}
