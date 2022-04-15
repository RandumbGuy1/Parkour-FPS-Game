using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShakeInducer : MonoBehaviour
{
    [Header("Shake settings")]
    [SerializeField] private float magnitude = 1f;
    [SerializeField] private float frequency = 1f;
    [SerializeField] private float duration = 1f;
    [SerializeField] private float smoothness = 1f;
    [SerializeField] private bool skipFirstEnable = false;

    void OnEnable()
    {
        if (skipFirstEnable)
        {
            skipFirstEnable = false;
            return;
        }

        ShakeManager.Instance.ShakeAll(new PerlinShake(ShakeData.Create(magnitude, frequency, duration, smoothness)), transform.position);
    }
}
