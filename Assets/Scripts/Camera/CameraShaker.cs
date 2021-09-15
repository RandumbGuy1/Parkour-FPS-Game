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

    public Vector3 Offset { get; private set; }
    private List<ShakeEvent> shakeEvents = new List<ShakeEvent>();

    public class ShakeEvent
    {
        public enum ShakeType
        {
            KickBack,
            Perlin,
        }

        private AnimationCurve blendOverLifetime;
        private ShakeType type;
        
        private float timeRemaining;
        private float duration;
        private float magnitude;
        private float frequency;
        private float trama = 0f;
        private float smoothness;

        private Vector3 noise = Vector3.zero;
        private Vector3 noiseOffset = Vector3.zero;

        private Vector3 targetDir = Vector3.zero;
        private Vector3 smoothDir = Vector3.zero;
        private Vector3 vel = Vector3.zero;

        public Vector3 Displacement { get; private set; } = Vector3.zero;
        public bool Finished { get { return timeRemaining <= 0f; } }

        public ShakeEvent(float magnitude, float frequency, float duration, float smoothness, AnimationCurve blendOverLifetime, ShakeType type, Vector3 initialKickback)
        {
            this.blendOverLifetime = blendOverLifetime;
            this.magnitude = magnitude;
            this.frequency = frequency;
            this.duration = duration;
            this.smoothness = smoothness;
            this.type = type;

            timeRemaining = this.duration;

            targetDir = initialKickback;

            noiseOffset.x = Random.Range(0f, 32f);
            noiseOffset.y = Random.Range(0f, 32f);
            noiseOffset.z = Random.Range(0f, 32f);
        }

        public void UpdateShake()
        {
            timeRemaining -= Time.deltaTime;

            switch (type)
            {
                case ShakeType.KickBack:
                    {
                        if ((targetDir - smoothDir).sqrMagnitude < (frequency * 0.1f)) targetDir = (-targetDir + Random.insideUnitSphere * 0.6f).normalized;
                        smoothDir = Vector3.SmoothDamp(smoothDir, targetDir, ref vel, smoothness);

                        Displacement = (smoothDir * magnitude) * trama;
                        break;
                    }

                case ShakeType.Perlin:
                    {
                        float offsetDelta = Time.deltaTime * frequency;

                        noiseOffset += Vector3.one * offsetDelta;

                        noise.x = Mathf.PerlinNoise(noiseOffset.x, 1f);
                        noise.y = Mathf.PerlinNoise(noiseOffset.y, 2f);
                        noise.z = Mathf.PerlinNoise(noiseOffset.z, 3f);

                        noise -= Vector3.one * 0.5f;
                        noise *= magnitude;
                        noise *= trama;

                        Displacement = Vector3.SmoothDamp(Displacement, noise, ref vel, smoothness);
                        break;
                    }
            }
            
            float agePercent = 1f - (timeRemaining / duration);
            trama = blendOverLifetime.Evaluate(agePercent);
            trama = Mathf.Clamp(trama, 0f, 1f);
        }
    }

    public void ShakeOnce(float magnitude, float frequency, float duration, float smoothness, ShakeEvent.ShakeType type, Vector3 initialKickback = default(Vector3)) => 
        shakeEvents.Add(new ShakeEvent(magnitude, frequency, duration, smoothness, blendOverLifetime, type, initialKickback));

    void LateUpdate()
    {
        Vector3 rotationOffset = Vector3.zero;

        if (shakeEvents.Count > 0)
        {
            for (int i = shakeEvents.Count - 1; i != -1; i--)
            {
                ShakeEvent shake = shakeEvents[i];

                if (shake.Finished)
                {
                    shakeEvents.RemoveAt(i);
                    continue;
                }

                shake.UpdateShake();
                rotationOffset += shake.Displacement;
            }
        }

        Offset = rotationOffset;
    }
}
