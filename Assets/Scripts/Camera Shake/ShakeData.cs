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
    public ShakeType type;
    public float magnitude = 1f;
    public float frequency = 1f;
    public float duration = 1f;
    public float smoothSpeed = 1f;
    [Space(10)]
    public AnimationCurve blendOverLifetime = new AnimationCurve(
      new Keyframe(0.0f, 0.0f, Mathf.Deg2Rad * 0.0f, Mathf.Deg2Rad * 720.0f),
      new Keyframe(0.2f, 1.0f),
      new Keyframe(1.0f, 0.0f));

    public void Intialize(float magnitude, float frequency, float duration, float smoothness, ShakeData.ShakeType type)
    {
        this.type = type;
        this.magnitude = magnitude;
        this.frequency = frequency;
        this.duration = duration;
        this.smoothSpeed = smoothness;
    }
}
