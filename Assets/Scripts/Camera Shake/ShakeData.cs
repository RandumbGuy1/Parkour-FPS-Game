using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Shake Data", menuName = "Custom Shake Event Data", order = 1)]
public class ShakeData : ScriptableObject
{
    public enum ShakeType
    {
        KickBack,
        Perlin,
    }

    [Header("Shake Settings")]
    [SerializeField] private ShakeType type;
    [SerializeField] private float magnitude = 1f;
    [SerializeField] private float frequency = 1f;
    [SerializeField] private float duration = 1f;
    [SerializeField] private float smoothSpeed = 1f;
    [Space(10)]
    [SerializeField] private AnimationCurve blendOverLifetime = new AnimationCurve(
      new Keyframe(0.0f, 0.0f, Mathf.Deg2Rad * 0.0f, Mathf.Deg2Rad * 720.0f),
      new Keyframe(0.2f, 1.0f),
      new Keyframe(1.0f, 0.0f));

    public ShakeType Type { get { return type; } }
    public float Magnitude { get { return magnitude; } }
    public float Frequency { get { return frequency; } }
    public float Duration { get { return duration; } }
    public float SmoothSpeed { get { return smoothSpeed; } }
    public AnimationCurve BlendOverLifetime { get { return blendOverLifetime; } }

    public void Intialize(float magnitude, float frequency, float duration, float smoothness, ShakeData.ShakeType type)
    {
        this.type = type;
        this.magnitude = magnitude;
        this.frequency = frequency;
        this.duration = duration;
        this.smoothSpeed = smoothness;
    }

    public void Initialize(ShakeData sd)
    {
        type = sd.Type;
        magnitude = sd.Magnitude;
        frequency = sd.Frequency;
        duration = sd.Duration;
        smoothSpeed = sd.SmoothSpeed;
    }
}
