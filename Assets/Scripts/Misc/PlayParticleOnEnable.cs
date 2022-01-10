using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayParticleOnEnable : MonoBehaviour
{
    [Header("Light Emission Settings")]
    [SerializeField] private float intensity;
    [SerializeField] private float intensitySmoothing;
    private float desiredIntensity;

    [Header("Assignables")]
    [SerializeField] private new Light light;
    private ParticleSystem particle;

    void Awake() => particle = GetComponent<ParticleSystem>();
    void OnEnable()
    {
        if (particle != null) particle.Play();

        if (light != null)
        {
            desiredIntensity = intensity;
            StartCoroutine(UpdateLight());
        }
    }

    private IEnumerator UpdateLight()
    {
        while (isActiveAndEnabled)
        {
            desiredIntensity = Mathf.Lerp(desiredIntensity, 0, intensitySmoothing * 0.5f * Time.deltaTime);
            light.intensity = Mathf.Lerp(light.intensity, desiredIntensity, intensitySmoothing * Time.deltaTime);

            yield return null;
        }
    }
}
