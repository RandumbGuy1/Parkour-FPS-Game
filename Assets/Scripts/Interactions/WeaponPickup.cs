using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponPickup : Interactable
{
    [Header("Pickup Settings")]
    [SerializeField] private float pickupPositionTime;
    [SerializeField] private float pickupRotationTime;

    [Header("Interaction Hint")]
    [SerializeField] private string description;

    private Rigidbody rb;
    private BoxCollider bc;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnDisable()
    {
        if (!rb.isKinematic) return;

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.Euler(0, 0, 0);
    }

    public override string GetDescription()
    {
        if (!rb.isKinematic) return description;
        else return null;
    }

    public override void OnInteract()
    {
        rb.isKinematic = true;
        rb.useGravity = false;
        StopAllCoroutines();
        StartCoroutine(Pickup());
    }

    private IEnumerator Pickup()
    {
        Vector3 vel = Vector3.zero;

        float posElapsed = 0f;
        float rotElapsed = 0f;

        float t = 0f;
        float u = 0f;

        while (rb.isKinematic)
        {
            if (posElapsed < pickupPositionTime)
            {
                t = posElapsed / pickupPositionTime;
                t = Mathf.Sin(t * Mathf.PI * 0.5f);

                transform.localPosition = Vector3.Lerp(transform.localPosition, Vector3.zero, t);
                posElapsed += Time.deltaTime;
            }

            if (rotElapsed < pickupRotationTime)
            {
                u = rotElapsed / pickupRotationTime;
                u = Mathf.Sin(u * Mathf.PI * 0.5f);

                transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(0, 0, 0), u);
                rotElapsed += Time.deltaTime;
            }

            if (posElapsed >= pickupPositionTime && rotElapsed >= pickupRotationTime)
            {
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.Euler(0, 0, 0);
                break;
            }

            yield return null;
        }
    }
}
