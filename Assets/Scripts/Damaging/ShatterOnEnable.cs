using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShatterOnEnable : MonoBehaviour
{
    [Header("Explosion Settings")]
    [SerializeField] private float explosionForce;
    [SerializeField] private float upwardsModifier;
    [SerializeField] private float explosionRadius;
    private bool started = false;

    private readonly List<Vector3> localPositions = new List<Vector3>();
    private readonly List<Quaternion> localRotations = new List<Quaternion>();

    void OnEnable()
    {
        if (!started) GetPositionsAndRotations();
        else SetPositionsAndRotations();

        Invoke(nameof(ExplodePieces), 0.05f);
    }

    void ExplodePieces()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Rigidbody rb = transform.GetChild(i).GetComponent<Rigidbody>();
            if (rb == null) return;

            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.angularVelocity = 10f * explosionForce * Random.insideUnitSphere;
            rb.AddExplosionForce(explosionForce, transform.position, upwardsModifier, explosionRadius, ForceMode.VelocityChange);
        }
    }

    void SetPositionsAndRotations()
    {
        if (localPositions.Count != transform.childCount && localRotations.Count != transform.childCount) return;

        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).localPosition = localPositions[i];
            transform.GetChild(i).localRotation = localRotations[i];
        }
    }

    void GetPositionsAndRotations()
    {
        started = true;

        for (int i = 0; i < transform.childCount; i++)
        {
            localPositions.Add(transform.GetChild(i).localPosition);
            localRotations.Add(transform.GetChild(i).localRotation);
        }
    }
}
