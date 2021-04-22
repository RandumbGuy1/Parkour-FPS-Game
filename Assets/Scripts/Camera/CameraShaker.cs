﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    [Header("Shake Settings")]
    public AnimationCurve blendOverLifetime = new AnimationCurve(

      new Keyframe(0.0f, 0.0f, Mathf.Deg2Rad * 0.0f, Mathf.Deg2Rad * 720.0f),
      new Keyframe(0.2f, 1.0f),
      new Keyframe(1.0f, 0.0f));

    public Vector3 offset { get; private set; }

    public class ShakeEvent
    {
        AnimationCurve blendOverLifetime;

        float timeRemaining;
        float duration;
        float magnitude;
        float frequency;
        float trama = 0f;

        float scroller = 0f;
        Vector3 noise;
        public Vector3 displacement;

        public ShakeEvent(float magnitude, float frequency, float duration, AnimationCurve blendOverLifetime)
        {
            this.blendOverLifetime = blendOverLifetime;
            this.magnitude = magnitude;
            this.frequency = frequency;
            this.duration = duration;
            timeRemaining = this.duration;
        }

        public void UpdateShake()
        {
            float offsetDelta = Time.deltaTime * frequency;

            timeRemaining -= Time.deltaTime;
            scroller += offsetDelta;

            noise.x = Mathf.PerlinNoise(0f, scroller);
            noise.y = Mathf.PerlinNoise(1f, scroller);
            noise.z = Mathf.PerlinNoise(2f, scroller);

            noise -= Vector3.one * 0.5f;
            noise *= magnitude;

            float agePercent = 1f - (timeRemaining / duration);
            trama = blendOverLifetime.Evaluate(agePercent);
            trama = Mathf.Clamp(trama, 0f, 1f);
            noise *= trama;

            displacement = noise;
        }

        public bool Alive()
        {
            return timeRemaining > 0f;
        }
    }

    List<ShakeEvent> shakeEvents = new List<ShakeEvent>();

    public void ShakeOnce(float magnitude, float frequency, float duration)
    {
        shakeEvents.Add(new ShakeEvent(magnitude, frequency, duration, blendOverLifetime));
    }

    void LateUpdate()
    {
        Vector3 rotationOffset = Vector3.zero;

        if (shakeEvents.Count > 0)
        {
            for (int i = shakeEvents.Count - 1; i != -1; i--)
            {
                ShakeEvent shake = shakeEvents[i]; 
                shake.UpdateShake();

                rotationOffset += shake.displacement;

                if (!shake.Alive()) shakeEvents.RemoveAt(i);
            }
        }

        offset = rotationOffset;
    }
}