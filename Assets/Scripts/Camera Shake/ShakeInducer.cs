using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShakeInducer : MonoBehaviour
{
    [Header("Shake settings")]
    [SerializeField] private ShakeData.ShakeType type;
    [SerializeField] private float magnitude = 1f;
    [SerializeField] private float frequency = 1f;
    [SerializeField] private float duration = 1f;
    [SerializeField] private float smoothness = 1f;
    [Space(10)]
    [SerializeField] private AnimationCurve blendOverLifetime = new AnimationCurve(
      new Keyframe(0.0f, 0.0f, Mathf.Deg2Rad * 0.0f, Mathf.Deg2Rad * 720.0f),
      new Keyframe(0.2f, 1.0f),
      new Keyframe(1.0f, 0.0f));

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            ShakeManager.Instance.ShakeAll(magnitude, frequency, duration, smoothness, type, transform.position);
        }
    }
}
