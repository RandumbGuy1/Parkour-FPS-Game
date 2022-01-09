using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShatterOnEnable : MonoBehaviour
{
    [Header("Explosion Settings")]
    [SerializeField] private float explosionForce;
    [SerializeField] private float upwardsModifier;
    [SerializeField] private float explosionRadius;

    void OnEnable()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Rigidbody rb = transform.GetChild(i).GetComponent<Rigidbody>();
            if (rb == null) return;

            rb.AddTorque(20f * explosionForce * Random.insideUnitSphere, ForceMode.VelocityChange);
            rb.AddExplosionForce(explosionForce, transform.position, upwardsModifier, explosionRadius, ForceMode.Impulse);
        }
    }
}
