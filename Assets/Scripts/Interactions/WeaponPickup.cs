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
        rb.interpolation = RigidbodyInterpolation.None;
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.detectCollisions = false;

        StopAllCoroutines();
        StartCoroutine(Pickup());
    }

    private IEnumerator Pickup()
    {
        Vector3 vel = Vector3.zero;

        while (transform.localPosition.sqrMagnitude > 0.01f || transform.localEulerAngles.sqrMagnitude > 0.01f)
        {
            transform.localPosition = Vector3.SmoothDamp(transform.localPosition, Vector3.zero, ref vel, pickupPositionTime);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(Vector3.zero), 1/pickupRotationTime * Time.deltaTime);

            if (!rb.isKinematic) break;

            yield return null;
        }

        if (rb.isKinematic)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.Euler(Vector3.zero);
        }
    }
}
