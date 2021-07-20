using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    [Header("Shake Settings")]
    [SerializeField] private AnimationCurve blendOverLifetime = new AnimationCurve(

      new Keyframe(0.0f, 0.0f, Mathf.Deg2Rad * 0.0f, Mathf.Deg2Rad * 720.0f),
      new Keyframe(0.2f, 1.0f),
      new Keyframe(1.0f, 0.0f));

    public Vector3 offset { get; private set; }
    private List<ShakeEvent> shakeEvents = new List<ShakeEvent>();

    public class ShakeEvent
    {
        AnimationCurve blendOverLifetime;

        float timeRemaining;
        float duration;
        float magnitude;
        float frequency;
        float trama = 0f;
        float smoothness;

        private Vector3 noise = Vector3.zero;
        private Vector3 noiseOffset = Vector3.zero;
        private Vector3 vel = Vector3.zero;

        public Vector3 displacement = Vector3.zero;

        public ShakeEvent(float magnitude, float frequency, float duration, float smoothness, float randomness, AnimationCurve blendOverLifetime)
        {
            this.blendOverLifetime = blendOverLifetime;
            this.magnitude = magnitude;
            this.frequency = frequency;
            this.duration = duration;
            this.smoothness = smoothness;

            timeRemaining = this.duration;

            noiseOffset.x = Random.Range(0f, randomness);
            noiseOffset.y = Random.Range(0f, randomness);
            noiseOffset.z = Random.Range(0f, randomness);
        }

        public void UpdateShake()
        {
            float offsetDelta = Time.deltaTime * frequency;
            timeRemaining -= Time.deltaTime;

            noiseOffset += Vector3.one * offsetDelta;

            noise.x = Mathf.PerlinNoise(noiseOffset.x, 1f);
            noise.y = Mathf.PerlinNoise(noiseOffset.y, 2f);
            noise.z = Mathf.PerlinNoise(noiseOffset.z, 3f);

            noise -= Vector3.one * 0.5f;
            noise *= magnitude;

            float agePercent = 1f - (timeRemaining / duration);
            trama = blendOverLifetime.Evaluate(agePercent);
            trama = Mathf.Clamp(trama, 0f, 1f);
            noise *= trama;

            displacement = Vector3.SmoothDamp(displacement, noise, ref vel, smoothness);
        }

        public bool Finished() => timeRemaining < 0f;
    }

    public void ShakeOnce(float magnitude, float frequency, float duration, float smoothness, float randomness) => shakeEvents.Add(new ShakeEvent(magnitude, frequency, duration, smoothness, randomness, blendOverLifetime));

    void LateUpdate()
    {
        Vector3 rotationOffset = Vector3.zero;

        if (shakeEvents.Count > 0)
        {
            for (int i = shakeEvents.Count - 1; i != -1; i--)
            {
                ShakeEvent shake = shakeEvents[i];

                if (shake.Finished())
                {
                    shakeEvents.RemoveAt(i);
                    continue;
                }

                shake.UpdateShake();
                rotationOffset += shake.displacement;
            }
        }

        offset = rotationOffset;
    }
}
