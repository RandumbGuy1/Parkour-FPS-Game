using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponPickup : Interactable
{
    [Header("Pickup Settings")]
    [SerializeField] private float pickupPositionTime;
    [SerializeField] private float pickupRotationSpeed;

    [Header("Interaction Hint")]
    [SerializeField] private string description;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override string GetDescription()
    {
        if (!rb.isKinematic) return description;
        else return null;
    }

    public override void OnInteract()
    {
        rb.isKinematic = true;
        StopAllCoroutines();
        StartCoroutine(Pickup());
    }

    private IEnumerator Pickup()
    {
        Vector3 targetPos = Vector3.zero;
        Quaternion targetRot = Quaternion.Euler(0, 0, 0);

        Vector3 vel = Vector3.zero;
        float elapsed = 0f;

        while (rb.isKinematic && elapsed < pickupPositionTime + (1 / pickupRotationSpeed) * 5f)
        {
            if (transform.localPosition != targetPos) transform.localPosition = Vector3.SmoothDamp(transform.localPosition, targetPos, ref vel, pickupPositionTime);
            if (transform.localRotation != targetRot) transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, pickupRotationSpeed * Time.deltaTime);
            elapsed += Time.deltaTime;

            yield return null;
        }
    }
}
