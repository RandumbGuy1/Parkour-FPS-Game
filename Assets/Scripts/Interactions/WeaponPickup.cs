using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponPickup : Interactable
{
    [Header("Pickup Settings")]
    [SerializeField] private float pickupPositionTime;
    [SerializeField] private float pickupRotationSpeed;

    private Vector3 vel;

    [Header("Interaction Hint")]
    [SerializeField] private string description;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (rb.isKinematic)
        {
            Vector3 targetPos = Vector3.zero;
            if (transform.localPosition != targetPos)
                transform.localPosition = Vector3.SmoothDamp(transform.localPosition, targetPos, ref vel, pickupPositionTime);

            Quaternion targetRot = Quaternion.Euler(0, 0, 0);
            if (transform.localRotation != targetRot)
                transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, pickupRotationSpeed * Time.deltaTime);
        }
    }

    public override string GetDescription()
    {
        if (!rb.isKinematic) return description;
        else return null;
    }

    public override void OnInteract()
    {
        rb.isKinematic = true;
    }

}
